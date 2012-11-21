using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using Zaypay.WebService;
using Zaypay;


namespace ZaypayPayalogueAndApiDemp.Models
{

    public class Purchase
    {
        public int ID { get; set; }
        public int ZaypayPaymentId { get; set; }
        public string Status { get; set; }        

        public virtual Product Product { get; set; }

        public Purchase(Product product)
        {            
            Product = product;
            Status = "prepared";             

        }

        public Purchase() { }

        public int PriceSettingId()
        {
            return Product.PriceSettingId;
        }

        public void Update(PaymentResponse payment)
        {
            if (payment != null)
            {
                Status = payment.Status();
                ZaypayPaymentId = payment.PaymentId();
            }

        }

    }

}