using BulkyBooks.DataAccess.Repositroy.IRepositroy;
using BulkyBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBooks.DataAccess.Repository.IRepositroy
{
    public interface IOrderHeaderRepository : IRepositroy<OrderHeader>
    {
        void Update(OrderHeader obj);
        void UpdateStatus(int id,string orderStatus,string? paymentStatus = null);
        public void UpdateStripePaymentId(int id, string sessionId, string paymentIntentId);

	}
}
