using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ZaypayPayalogueAndApiDemp.Models;

namespace ZaypayPayalogueAndApiDemp.Controllers
{ 
    public class ProductsController : Controller
    {
        private ZaypayDBContext db = new ZaypayDBContext();

        //
        // GET: /Products/

        public ViewResult Index()
        {
            return View(db.Products.ToList());
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}