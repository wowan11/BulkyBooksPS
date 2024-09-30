using BulkyBooks.DataAccess.Repository.IRepositroy;
using BulkyBooks.Models;
using BulkyBooks.Models.ViewModels;
using BulkyBooks.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol.Core.Types;
using Stripe;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyBooksWeb.Areas.Admin.Controllers
{
	[Area("Admin")]
    [Authorize]
    public class OrderController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;

        [BindProperty]
        public OrderVM OrderVM { get; set; }

        public OrderController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}
		public IActionResult Index()
		{
			return View();
		}

        public IActionResult Details(int orderId)
        {
            OrderVM = new OrderVM()
            {
                orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x=>x.Id == orderId, includProperties: "ApplicationUser"),
                orderDetails =_unitOfWork.OrderDetail.GetAll(x=>x.OrderId == orderId, includProperties: "Product")
            };
            return View(OrderVM);
        }

        [ActionName("Details")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Details_Pay_Now()
        {
            OrderVM.orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == OrderVM.orderHeader.Id, includProperties: "ApplicationUser");
            OrderVM.orderDetails = _unitOfWork.OrderDetail.GetAll(x => x.OrderId == OrderVM.orderHeader.Id, includProperties: "Product");


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
                SuccessUrl = domain + $"/admin/order/PaymentConfirmation?OrderHeaderid={OrderVM.orderHeader.Id}",
                CancelUrl = domain + $"/admin/order/details?orderId={OrderVM.orderHeader.Id}",
            };

            foreach (var item in OrderVM.orderDetails)
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

            _unitOfWork.OrderHeader.UpdateStripePaymentId(OrderVM.orderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);

        }


        public IActionResult PaymentConfirmation(int OrderHeaderid)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == OrderHeaderid);


            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                //connecting to EXISTing service based on ID
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                //check the STRIPE STATUS(does payment done successfully)

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentId(OrderHeaderid, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(OrderHeaderid, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }
            return View(OrderHeaderid);
        }


        [HttpPost]
        [Authorize(Roles =SD.Role_Admin + "," + SD.Role_Emoloyee)]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateOrderDetail ()
        {
            var orderHeaderFromDB = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == OrderVM.orderHeader.Id,tracked:false);
            orderHeaderFromDB.Name= OrderVM.orderHeader.Name;
            orderHeaderFromDB.PhoneNumber= OrderVM.orderHeader.PhoneNumber;
            orderHeaderFromDB.StreetAddress= OrderVM.orderHeader.StreetAddress;
            orderHeaderFromDB.City= OrderVM.orderHeader.City;
            orderHeaderFromDB.State= OrderVM.orderHeader.State; 
            orderHeaderFromDB.PostalCode= OrderVM.orderHeader.PostalCode;
            
            if (OrderVM.orderHeader.Carrier!=null)
            {
                orderHeaderFromDB.Carrier=OrderVM.orderHeader.Carrier;
            }
            if (OrderVM.orderHeader.TrackingNumber != null)
            {
                orderHeaderFromDB.TrackingNumber = OrderVM.orderHeader.TrackingNumber;
            }

            _unitOfWork.OrderHeader.Update(orderHeaderFromDB);
            _unitOfWork.Save();
            TempData["Success"] = "Order Details Updated successfully";
            return RedirectToAction("Details","Order", new { orderId = orderHeaderFromDB.Id});
        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Emoloyee)]
        [ValidateAntiForgeryToken]
        public IActionResult StartProcessing()
        {
            _unitOfWork.OrderHeader.UpdateStatus(OrderVM.orderHeader.Id,SD.StatusInProcess);
            _unitOfWork.Save();
            TempData["Success"] = "Order Status Updated successfully";
            return RedirectToAction("Details", "Order", new { orderId = OrderVM.orderHeader.Id });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ShipOrder()
        {
            var orderHeaderFromDBB = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == OrderVM.orderHeader.Id, tracked: false);
            orderHeaderFromDBB.TrackingNumber= OrderVM.orderHeader.TrackingNumber;
            orderHeaderFromDBB.Carrier = OrderVM.orderHeader.Carrier;
            orderHeaderFromDBB.OrderStatus= SD.StatusShipped;
            orderHeaderFromDBB.ShippingDate= DateTime.Now;
           
            if (orderHeaderFromDBB.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeaderFromDBB.PaymentDueDate = DateTime.Now.AddDays(30);
            }

            _unitOfWork.OrderHeader.Update(orderHeaderFromDBB);
            _unitOfWork.Save();
            TempData["Success"] = "Order Shipped  successfully";
            return RedirectToAction("Details", "Order", new { orderId = OrderVM.orderHeader.Id });
        }



        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Emoloyee)]
        [ValidateAntiForgeryToken]
        public IActionResult CancelOrder()
        {
            var orderHeaderFromDBB = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == OrderVM.orderHeader.Id, tracked: false);

            if(orderHeaderFromDBB.PaymentStatus == SD.StatusApproved)//if PAYMENT ALREADY DONE
            {
                //we will have to REFUND THAT
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeaderFromDBB.PaymentIntentId
                    //Amount=1000
                    //will by default REFUND EXACT AMOUNT OF MONEY that WAS IN BOOK ORDER
                    //you can add CUSTOM Amount of money that will REFUND
                };

                var service = new RefundService();
                Refund refund = service.Create(options);

                _unitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDBB.Id, SD.StatusCancelled,SD.StatusRefunded);

            }
            else
            {   //if PAYMENT NOT DONE we just updating status (dont have refund process)
                _unitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDBB.Id, SD.StatusCancelled, SD.StatusCancelled);
            }

            _unitOfWork.Save();
            TempData["Success"] = "Order Cancelled  successfully";
            return RedirectToAction("Details", "Order", new { orderId = OrderVM.orderHeader.Id });
        }




        //IMPLEMET DATATABLE(preset table for UI) 
        #region API CALLS
        [HttpGet]
		public IActionResult GetAll(string status)
		{
			IEnumerable<OrderHeader> orderHeaders;
			if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Emoloyee))
			{
				orderHeaders = _unitOfWork.OrderHeader.GetAll(includProperties: "ApplicationUser");
			}
			else
			{
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);//extracting Name
																					//extracting Id Of USER that now is LOGIN
                orderHeaders = _unitOfWork.OrderHeader.GetAll(x=>x.ApplicationUserId==claim.Value, includProperties: "ApplicationUser");
            }

            switch (status)
            {
                case "pending":
                    orderHeaders = orderHeaders.Where(x=>x.PaymentStatus==SD.PaymentStatusDelayedPayment);
                    break;
				case "inprocess":
                    orderHeaders = orderHeaders.Where(x => x.OrderStatus == SD.StatusInProcess);
                    break;
                case "complited":
                    orderHeaders = orderHeaders.Where(x => x.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    orderHeaders = orderHeaders.Where(x => x.OrderStatus == SD.StatusApproved);
                    break;
                default:
                    break;
            }

            return Json(new { data = orderHeaders });
			//явное Приведение к Json
		}
		#endregion
	}
}
