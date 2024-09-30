using BulkyBooks.DataAccess.Repositroy.IRepositroy;
using BulkyBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBooks.DataAccess.Repository.IRepositroy
{
    public interface IProductRepository : IRepositroy<Product>
    {
        void Update(Product obj);

    }
}
