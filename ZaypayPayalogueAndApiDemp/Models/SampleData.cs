using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace ZaypayPayalogueAndApiDemp.Models
{
    public class SampleData : DropCreateDatabaseIfModelChanges<ZaypayDBContext>
    {
        protected override void Seed(ZaypayDBContext context)
        {
            new List<Product>
            {
                new Product{ ID = 1, Name = "10 Points Package", Description = "10 points package description", PayalogueId = 125914, PriceSettingId = 140494 },
                new Product{ ID = 2, Name = "30 Points Package", Description = "30 points package description", PayalogueId = 125914, PriceSettingId = 140494 }
            }.ForEach(i => context.Products.Add(i));

            context.SaveChanges();

        }
    }
}