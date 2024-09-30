using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBooks.DataAccess.Repositroy.IRepositroy
{
    public interface IRepositroy<T> where T : class
    {
        //T- Category
        T GetFirstOrDefault(Expression <Func<T,bool>> filter, string? includProperties = null,bool tracked = true);//GET method //WHY WE PASSING FUNC
        IEnumerable<T> GetAll(Expression<Func<T, bool>>? filter = null,string ? includProperties = null); //like our GET method for Index
        void Add(T entity);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities); //IEnumerable<T> because we removing MULTIPLE FILES

    }

}
