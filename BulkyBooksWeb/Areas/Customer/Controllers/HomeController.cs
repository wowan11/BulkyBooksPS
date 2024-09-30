using BulkyBooks.DataAccess;
using BulkyBooks.DataAccess.Repository.IRepositroy;
using BulkyBooks.Models;
using BulkyBooks.Models.ViewModels;
using BulkyBooks.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyBooksWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private IUnitOfWork _unitOfWork;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IUnitOfWork unitOfWork, ILogger<HomeController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> productList = _unitOfWork.Product.GetAll(includProperties:"Category,CoverType"); 
            return View(productList);
        }

        public IActionResult Details(int productId)
        {

            ShoppingCart cartObj = new()
            {
                Count = 1,
                ProductId = productId,
                Product = _unitOfWork.Product.GetFirstOrDefault(u => u.Id == productId, includProperties: "Category,CoverType")
            };
            return View(cartObj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] //only Authorize user can access to Details method
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);//extracting Name
            shoppingCart.ApplicationUserId = claim.Value;//extracting Id Of USER that now is buying

            ShoppingCart cartFromDB = _unitOfWork.ShoppingCart.GetFirstOrDefault(
                x => x.ApplicationUserId == claim.Value && x.ProductId == shoppingCart.ProductId
                );

            if (cartFromDB == null)
            {
                _unitOfWork.ShoppingCart.Add(shoppingCart);
                _unitOfWork.Save();
                HttpContext.Session.SetInt32(SD.SessionCart,
                    _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value).ToList().Count);
                //temp itn to SEE WHAT INSIDE SESSION
                //int? tempint = HttpContext.Session.GetInt32(SD.SessionCart);
            }
            else
            {
                _unitOfWork.ShoppingCart.IncrementCount(cartFromDB, shoppingCart.Count);
                _unitOfWork.Save();
            }
 

            return RedirectToAction(nameof(Index));
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}