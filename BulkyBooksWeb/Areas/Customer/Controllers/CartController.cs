using BulkyBooks.DataAccess.Repository.IRepositroy;
using BulkyBooks.Models;
using BulkyBooks.Models.ViewModels;
using BulkyBooks.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Options;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyBooksWeb.Areas.Customer.Controllers
{
	[Area("Customer")]
	[Authorize]
	public class CartController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		public readonly IEmailSender _emailSender;

		[BindProperty]
		public ShoppingCartVM shoppingCartVM { get; set; }

		public int OrderTotal { get; set; }
		public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender)
		{
			_unitOfWork = unitOfWork;
			_emailSender = emailSender;
		}

		public IActionResult Index()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);//extracting Name

			shoppingCartVM = new ShoppingCartVM()
			{                                                                           //claim -- extracting Id Of USER that now is buying
				ListCart = _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == claim.Value, includProperties: "Product"),
				OrderHeader = new OrderHeader()
			};

			foreach (var item in shoppingCartVM.ListCart)
			{
				item.Price = GetPriceBasedOnQuantity(item.Count, item.Product.Price, item.Product.Price50, item.Product.Price100);
				shoppingCartVM.OrderHeader.OrderTotal += (item.Price * item.Count);
			}
			return View(shoppingCartVM);
		}



		private double GetPriceBasedOnQuantity(double quantity, double price, double price50, double price100)
		{
			if (quantity <= 50)
			{
				return price;
			}
			else
			{
				if (quantity <= 100)
				{
					return price50;
				}
				return price100;
			}
		}

		public IActionResult Plus(int cartId)
		{
			var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(x => x.Id == cartId);
			_unitOfWork.ShoppingCart.IncrementCount(cart, 1);
			_unitOfWork.Save();
			return RedirectToAction(nameof(Index));
		}
		public IActionResult Minus(int cartId)
		{
			var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(x => x.Id == cartId);
			if (cart.Count <= 1)
			{
				_unitOfWork.ShoppingCart.Remove(cart);
                var count = _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == cart.ApplicationUserId).ToList().Count-1;
                HttpContext.Session.SetInt32(SD.SessionCart, count);
            }
			else
			{
				_unitOfWork.ShoppingCart.DecrementCount(cart, 1);
			}
			_unitOfWork.Save();
			return RedirectToAction(nameof(Index));
		}
		public IActionResult Remove(int cartId)
		{
			var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(x => x.Id == cartId);
			_unitOfWork.ShoppingCart.Remove(cart);
			_unitOfWork.Save();
			var count =_unitOfWork.ShoppingCart.GetAll(x=>x.ApplicationUserId==cart.ApplicationUserId).ToList().Count;
			HttpContext.Session.SetInt32(SD.SessionCart, count);
			return RedirectToAction(nameof(Index));
		}



		public IActionResult Summary()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);//extracting Name

			shoppingCartVM = new ShoppingCartVM()
			{                                                                           //claim -- extracting Id Of USER that now is buying
				ListCart = _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == claim.Value, includProperties: "Product"),
				OrderHeader = new OrderHeader()
			};

			shoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(x => x.Id == claim.Value);

			shoppingCartVM.OrderHeader.Name = shoppingCartVM.OrderHeader.ApplicationUser.Name;
			shoppingCartVM.OrderHeader.PhoneNumber = shoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
			shoppingCartVM.OrderHeader.StreetAddress = shoppingCartVM.OrderHeader.ApplicationUser.StreetAdress;
			shoppingCartVM.OrderHeader.City = shoppingCartVM.OrderHeader.ApplicationUser.City;
			shoppingCartVM.OrderHeader.State = shoppingCartVM.OrderHeader.ApplicationUser.State;
			shoppingCartVM.OrderHeader.PostalCode = shoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

			foreach (var item in shoppingCartVM.ListCart)
			{
				item.Price = GetPriceBasedOnQuantity(item.Count, item.Product.Price, item.Product.Price50, item.Product.Price100);
				shoppingCartVM.OrderHeader.OrderTotal += (item.Price * item.Count);
			}
			return View(shoppingCartVM);
		}

		[HttpPost]
		[ActionName("Summary")]
		[ValidateAntiForgeryToken]
		public IActionResult SummaryPOST()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);//extracting Name


			//claim -- extracting Id Of USER that NOW is buying
			shoppingCartVM.ListCart = _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == claim.Value, includProperties: "Product");


			shoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
			shoppingCartVM.OrderHeader.ApplicationUserId = claim.Value;


			foreach (var item in shoppingCartVM.ListCart)
			{
				item.Price = GetPriceBasedOnQuantity(item.Count, item.Product.Price, item.Product.Price50, item.Product.Price100);
				shoppingCartVM.OrderHeader.OrderTotal += (item.Price * item.Count);
			}


			//checking is company ASSAIGN(log in) or not
			//IF its company CompanyId should NOT BE 0(it will be generated and be 1 or 5 or 8.....)
			//IF CompanyId == 0 its just USER so it will have DIFFERENT STATUS and STRIPE PROCESS

			//claim -- extracting Id Of USER that now is buying
			ApplicationUser applicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(x => x.Id == claim.Value);

			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
				shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
				shoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
			}
			else
			{
				shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
				shoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
			}

			_unitOfWork.OrderHeader.Add(shoppingCartVM.OrderHeader);
			_unitOfWork.Save();


			foreach (var item in shoppingCartVM.ListCart)
			{
				var orderDetail = new OrderDetail()
				{
					ProductId = item.ProductId,
					OrderId = shoppingCartVM.OrderHeader.Id,
					Price = item.Price,
					Count = item.Count,
				};
				_unitOfWork.OrderDetail.Add(orderDetail);
				_unitOfWork.Save();
			}



			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{

				//stripe settings
				var domain = "https://localhost:44364";
				var options = new SessionCreateOptions
				{
					PaymentMethodTypes = new List<string>
				{
					"card",
				},
					LineItems = new List<SessionLineItemOptions>(),
					Mode = "payment",                       //OrderConfirmation method will create later 
					SuccessUrl = domain + $"/customer/cart/OrderConfirmation?id={shoppingCartVM.OrderHeader.Id}",
					CancelUrl = domain + "/cart/index",
				};

				foreach (var item in shoppingCartVM.ListCart)
				{
					var sessionLineItem = new SessionLineItemOptions
					{
						PriceData = new SessionLineItemPriceDataOptions
						{
							// Provide the exact Price ID (for example, pr_1234) of the product you want to sell
							UnitAmount = (long)(item.Price * 100), //UnitAmount = 2000    20.00 *100 = 2000 because its double we shoul multiply by 100
							Currency = "usd",
							ProductData = new SessionLineItemPriceDataProductDataOptions
							{
								Name = item.Product.Title
							}
						},
						Quantity = item.Count,
					};
					options.LineItems.Add(sessionLineItem);
				}
				var service = new SessionService();
				Session session = service.Create(options);

				_unitOfWork.OrderHeader.UpdateStripePaymentId(shoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
				_unitOfWork.Save();

				Response.Headers.Add("Location", session.Url);
				return new StatusCodeResult(303);
			}
			else
			{
				return RedirectToAction("OrderConfirmation","Cart",new {shoppingCartVM.OrderHeader.Id});
			}
		}
			
		public IActionResult OrderConfirmation(int id)
		{
			OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == id,includProperties: "ApplicationUser");


			if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
			{
				//connecting to EXISTing service based on ID
				var service = new SessionService();
				Session session = service.Get(orderHeader.SessionId);

				//check the STRIPE STATUS(does payment done successfully)

				if (session.PaymentStatus.ToLower() == "paid")
				{
                    _unitOfWork.OrderHeader.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
					_unitOfWork.Save();
				}
			}

			_emailSender.SendEmailAsync(orderHeader.ApplicationUser.Email,"Topic of our message","<p>New Order Created</p>");
			List<ShoppingCart> shoppingCarts11 = _unitOfWork.ShoppingCart.GetAll(u=>u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

			HttpContext.Session.Clear();	
			_unitOfWork.ShoppingCart.RemoveRange(shoppingCarts11);
			_unitOfWork.Save();
			return View(id);
		}
	}
}
