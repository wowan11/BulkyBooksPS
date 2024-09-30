using BulkyBooks.DataAccess.Repository.IRepositroy;
using BulkyBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBooks.DataAccess.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private ApplicationDbContext _db;

        public ProductRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }


        public void Update(Product obj)
        {
            var objFromDB = _db.Products.FirstOrDefault(u => u.Id == obj.Id);
            if (objFromDB != null)
            {
                objFromDB.Title = obj.Title;
                objFromDB.ISBN = obj.ISBN;
                objFromDB.Price= obj.Price;
                objFromDB.Price50= obj.Price50;
                objFromDB.ListPrice = obj.ListPrice;
                objFromDB.Price100= obj.Price100;
                objFromDB.Description = obj.Description;
                objFromDB.CategoryId = obj.CategoryId;
                objFromDB.Author = obj.Author;
                objFromDB.CoverTypeId= obj.CoverTypeId;
                if(objFromDB.ImageURL!= null)
                {
                    objFromDB.ImageURL = obj.ImageURL;
                }
                

            }
        }

    }
}
