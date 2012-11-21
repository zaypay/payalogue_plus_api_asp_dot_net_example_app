﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace ZaypayPayalogueAndApiDemp.Models
{
    public class Product
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int PriceSettingId { get; set; }
        public int PayalogueId { get; set; }

        public virtual ICollection<Purchase> Purchases { get; set; }

    }
}