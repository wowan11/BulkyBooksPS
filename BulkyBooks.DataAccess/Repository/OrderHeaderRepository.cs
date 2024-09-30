using BulkyBooks.DataAccess.Repository.IRepositroy;
using BulkyBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBooks.DataAccess.Repository
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
	{
        private ApplicationDbContext _db;

        public OrderHeaderRepository(ApplicationDbContext db) : base(db) 
        {
            _db= db;
        }


        public void Update(OrderHeader obj)
        {
            _db.OrderHeaders.Update(obj);
        }

		public void UpdateStatus(int id, string orderStatus, string? paymentStatus = null)
		{
            var orderFromDB = _db.OrderHeaders.FirstOrDefault(x => x.Id == id);

            if (orderFromDB != null)
            {
				orderFromDB.OrderStatus = orderStatus;

                if (paymentStatus != null)
                {
                    orderFromDB.PaymentStatus = paymentStatus;
                }

			}
		}

		public void UpdateStripePaymentId(int id, string sessionId, string paymentIntentId)
		{
			var orderFromDB = _db.OrderHeaders.FirstOrDefault(x => x.Id == id);

            orderFromDB.PaymentDate= DateTime.Now;
			orderFromDB.SessionId = sessionId;
			orderFromDB.PaymentIntentId = paymentIntentId;

		}
	}
}
