using BulkyBooks.DataAccess;
using BulkyBooks.DataAccess.Repository.IRepositroy;
using BulkyBooks.Models;
using BulkyBooks.Models.ViewModels;
using BulkyBooks.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Stripe.Radar;
using System.Data;

namespace BulkyBooksWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        //See all info from DB
        public IActionResult Index()
        {
            return View();
        }


        //GET
        [HttpGet]
        public IActionResult Upsert(int? id)
        {
            var company = new Company();

            if (id == null || id == 0)
            {   
                //create product if id is null(its wasnt exist early)
                return View(company);
            }
            else
            {
                //update product
                company = _unitOfWork.Company.GetFirstOrDefault(u=>u.Id==id);
                return View(company);

            }

        }


        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Company obj, IFormFile? file)
        {
            if (ModelState.IsValid)
            {

                if (obj.Id == 0)
                {
                    _unitOfWork.Company.Add(obj);
                    TempData["success"] = "Company ADD is working";
                }
                else
                {
                    _unitOfWork.Company.Update(obj);
                    TempData["update"] = "Company UPDATE is working";
                }

                _unitOfWork.Save();
                return RedirectToAction("Index");
                }
            return View(obj);   
        }




        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var companyList = _unitOfWork.Company.GetAll();
            return Json(new {data = companyList});
            //явное Приведение к Json
        }

        //POST
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var obj = _unitOfWork.Company.GetFirstOrDefault(i => i.Id == id);
            if (obj == null)
            {
                return Json(new { success= false,message="ERROR while DELETING"});
            }

            

            _unitOfWork.Company.Remove(obj);
            _unitOfWork.Save();
            return Json(new { success = true, message = "DELETING IS GOOD" });

        }
        #endregion

    }
}
