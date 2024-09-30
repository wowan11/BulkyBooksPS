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
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment hostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _hostEnvironment = hostEnvironment;
        }



        //See all info from DB
        public IActionResult Index()
        {
            return View();
            //old way WITOUT API EndPoint 
            //IEnumerable<CoverType> objCoverTypeList = _unitOfWork.CoverType.GetAll();
            //return View(objCoverTypeList);
        }






        //GET
        [HttpGet]
        public IActionResult Upsert(int? id)
        {
            var productVM = new ProductVM()
            {
                Product = new Product(),
                CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),
                CoverTypeList = _unitOfWork.CoverType.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),
            };
            if (id == null || id == 0)
            {   
                //create product if id is null(its wasnt exist early)
                //ViewBag.CategoryList = CategoryList;
                //ViewData["CoverTypeList"] = CoverTypeList;
                return View(productVM);
            }
            else
            {
                //update product
                productVM.Product= _unitOfWork.Product.GetFirstOrDefault(u=>u.Id==id);
                return View(productVM);

            }

        }


        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductVM obj, IFormFile? file)
        {
                if (ModelState.IsValid)
                {
                    //IFormFile? file --- is Image (check in html)
                    //getting Path
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                
                    if (file != null)
                    {
                        //Generating new file NAME
                        //TO SOLVE problem when you upload TWO images
                        //with SAME NAME (using Guid will be always uniqe name)
                        string fileName = Guid.NewGuid().ToString();
                        //find out final location where image needs to be uploaded
                        var uploads = Path.Combine(wwwRootPath, @"Images\Product");
                        //RENAME file
                        var extension = Path.GetExtension(file.FileName);


                    //if image file ALREADY exists WE SHOULD DELETE IT
                    if (obj.Product.ImageURL != null)
                    {
                        //removing / symbol from path str 
                        var oldImagePath = Path.Combine(wwwRootPath,obj.Product.ImageURL.TrimStart('\\'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }
                
                        //COPYing FILE that was uploaded INSIDE product folder
                        //var fileStream is our path
                        using(var fileStreams = new FileStream(Path.Combine(uploads,fileName+extension),FileMode.Create))
                        {   
                            file.CopyTo(fileStreams);
                        }
                        //Changing our object
                        obj.Product.ImageURL = @"\Images\Product\" + fileName + extension;
                
                    }
                if (obj.Product.Id == 0)
                {
                    _unitOfWork.Product.Add(obj.Product);
                }
                else
                {
                    _unitOfWork.Product.Update(obj.Product);
                }

                    _unitOfWork.Save();
                    TempData["success"] = "Product ADD is working";
                    return RedirectToAction("Index");
                }
            return View(obj);   
        }



		//IMPLEMET DATATABLE(preset table for UI) 
		#region API CALLS
		[HttpGet]
        public IActionResult GetAll()
        {
            var productList = _unitOfWork.Product.GetAll(includProperties: "Category,CoverType");
            return Json(new {data = productList });
            //явное Приведение к Json
        }

        //POST
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var obj = _unitOfWork.Product.GetFirstOrDefault(i => i.Id == id);
            if (obj == null)
            {
                return Json(new { success= false,message="ERROR while DELETING"});
            }

            
            var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, obj.ImageURL.TrimStart('\\'));
            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
            }

            _unitOfWork.Product.Remove(obj);
            _unitOfWork.Save();
            return Json(new { success = true, message = "DELETING IS GOOD" });

        }
        #endregion

    }
}
