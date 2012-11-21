using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Collections;
using Zaypay.WebService;
using Zaypay;
using System.Collections.Specialized;
using System.Web.Routing;
using ZaypayPayalogueAndApiDemp.Models;
using System.Xml;
using System.Text.RegularExpressions;
using Microsoft.Practices.EnterpriseLibrary.Logging;

namespace ZaypayPayalogueAndApiDemp.Controllers
{ 
    public class PurchasesController : Controller
    {
        private ZaypayDBContext db = new ZaypayDBContext();
        LogEntry logEntry = new LogEntry();

        public ActionResult GetPaymentMethods()
        {
            int id = 0;
            Int32.TryParse(Request.Form["productId"], out id);

            Product product = db.Products.First(m => m.ID == (id));

            if (product != null)
            {
                PriceSetting ps = new PriceSetting(product.PriceSettingId);
                string locale = Request.Form["language"] + "-" + Request.Form["country"];
                ps.LOCALE = locale;
                PaymentMethodResponse paymentObject = ps.ListPaymentMethods();

                if (paymentObject.RESPONSE.ContainsKey("error"))
                {
                    return Json(new { success = false, message = "Error" }, JsonRequestBehavior.AllowGet);

                }
                else
                {
                    List<Hashtable> paymentMethods = paymentObject.PaymentMethods();
                    return PartialView("_PaymentMethodButtons", paymentMethods);
                }
            }
            else
            {
                return Json(new { success = false, message = "Error" }, JsonRequestBehavior.AllowGet);
            }

        }

        //
        // GET: /Purchases/Create
        // GET: /Purchase/Create?id=?
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult Create(int productId = 0)
        {

            //Product product = db.Products.First(m => m.ID == productId);
            Product product = db.Products.Find(productId);

            if (product != null)
            {
                try
                {
                    PriceSetting ps = new PriceSetting(product.PriceSettingId);

                    //check if locale is supported 
                    string userIp = GetUserIP();
                    string locale = "";
                    string[] locales = "en-".Split('-');

                    ListLocalesResponse localesList = ps.ListLocales();
                    
                    //if we have correct user ip
                    if (!String.IsNullOrWhiteSpace(userIp))
                    {
                        LocalForIPResponse localeResponse = ps.LocaleForIP(userIp);
                        locale = localeResponse.Locale();

                        ps.LOCALE = locale;
                        locales = locale.Split('-');

                        if (localesList.CountrySupported(locales[1]))
                        {
                            List<Hashtable> paymentMethods = ps.ListPaymentMethods().PaymentMethods();
                            ViewData.Add("paymentMethods", paymentMethods);
                        }
                    }
                    else
                    {
                        ViewData["ipIsNull"] = true;
                    }

                    var countriesHash = (List<Hashtable>)localesList.Countries();
                    var languagesHash = (List<Hashtable>)localesList.Languages();

                    List<SelectListItem> countriesList = GetCountries(ref countriesHash);
                    countriesList = countriesList.OrderBy(x => x.Text).ToList();

                    List<SelectListItem> languagesList = GetLanguages(ref languagesHash);
                    languagesList = languagesList.OrderBy(x => x.Text).ToList();

                    ViewData.Add("countries", new SelectList(countriesList, "Value", "Text", locales[1]));
                    ViewData.Add("languages", new SelectList(languagesList, "Value", "Text", locales[0]));

                    if (TempData["error"] != null)
                        ViewData["error"] = TempData["error"];

                    return View(product);
                }
                catch (Exception e)
                {
                    string mesg = GetExceptionMessage(e);

                    if (mesg == "")
                        throw e;
                    else
                    {
                        LogEntry(e.Message);
                        ViewData["error"] = mesg;
                        return View("../products/index", db.Products.ToList());
                    }
                }
                
            }
            else
            {
                throw new HttpException(404, "Sorry, Resource not available");
            }

        }

        
        public ActionResult Reporting()
        {

            NameValueCollection parameters = Request.QueryString;
            
            //check if all required params are there
            if (AllValuesPresent(ref parameters))
            {
                
                int priceSettingId = 0;
                int paymentId = 0;
                int purchaseId = 0;
                int payalogueId = 0;

                Int32.TryParse(parameters["price_setting_id"], out priceSettingId);
                Int32.TryParse(parameters["payment_id"], out paymentId);
                Int32.TryParse(parameters["purchase_id"], out purchaseId);
                Int32.TryParse(parameters["payalogue_id"], out payalogueId);

                Purchase purchase = db.Purchases.Find(purchaseId);
                if (purchase != null && (purchase.ZaypayPaymentId == paymentId))
                {
                
                    Product product = purchase.Product;

                    if (product.PriceSettingId == priceSettingId && product.PayalogueId == payalogueId)
                    {
                
                        PriceSetting ps = new PriceSetting(product.PriceSettingId);

                        PaymentResponse response = ps.ShowPayment(purchase.ZaypayPaymentId);
                        string status = response.Status();
                
                        if (status == parameters["status"])
                        {                            
                            purchase.Status = status;
                            db.SaveChanges();

                        }
                    }

                }

            }
            else
            {
                LogEntry("Reporting Method in Purchase Controller ----  Values are missing --- the request had following query string :: " + parameters);
                System.Diagnostics.Debug.WriteLine("VAL NOT PRESENT");
            }
            return Content("*ok*");

        }

        //
        // POST: /Purchases/Create

        [HttpPost]
        public ActionResult Create(FormCollection form)
        {

            int productId = 0;
            int paymentMethodId = 0;

            Int32.TryParse(form["productId"], out productId);

            Product product = db.Products.Find(productId);

            if (product != null)
            {
                if (Int32.TryParse(form["paymentMethod"], out paymentMethodId) && form["languagesList"] != null && form["countriesList"] != null)
                {
                    Purchase purchase = null;

                    try
                    {

                        PriceSetting ps = new PriceSetting(product.PriceSettingId);
                           
                        string locale = form["languagesList"] + "-" + form["countriesList"];

                        ps.LOCALE = locale;
                        ps.PAYMENT_METHOD_ID = paymentMethodId;
                        ps.PAYALOGUE_ID = product.PayalogueId;

                        purchase = CreatePurchase(product);
                        
                        NameValueCollection options = new NameValueCollection();
                        options.Add("purchase_id", purchase.ID.ToString());
                        PaymentResponse payment = ps.CreatePayment(options);

                        purchase.Update(payment);
                        db.SaveChanges();

                        return Redirect("https://secure.zaypay.com" + payment.PayalogueUrl());

                    }
                    catch (Exception e)
                    {
                        string mesg = GetExceptionMessage(e);

                        if (purchase != null)
                            RemovePurchase(ref purchase);

                        if (mesg == "")
                            throw e;
                        else
                        {
                            LogEntry(e.Message);
                            TempData["error"] = mesg;
                            return RedirectToAction("Create", new { productId = product.ID });
                        }
                    }

                }
                else
                {
                    TempData["error"] = "Language , Country or Payment Method is not selected properly";
                    return RedirectToAction("Create", new { productId = product.ID });
                }
            }
            else
            {
                throw new HttpException(404, "Sorry, Resource Not Found");
            }

            //return RedirectToAction("Details",new { id = purchase.ID});


        }

        // ========================================================================================
        // PROTECTED METHODS
        // ========================================================================================

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }


        // ========================================================================================
        // PRIVATE METHODS
        // ========================================================================================

       
        private Purchase CreatePurchase(Product product)
        {
            Purchase purchase = new Purchase(product);
            db.Purchases.Add(purchase);
            db.SaveChanges();
            return purchase;
        }

        private string GetExceptionMessage(Exception ex)
        {
            string mesg = "";

            if (ex is System.Net.WebException)
            {
                mesg = "Payment server error. Try again !";
            }
            else if (ex is XmlException)
            {
                mesg = "Payment server config error. Try again !";
            }
            else
            {
                if (ex.Data.Contains("type"))
                {
                    if (ex.Data.Contains("user_message"))
                        mesg = ex.Data["user_message"].ToString();
                    else
                        mesg = "Config Error Occured";
                }
            }

            return mesg;

        }


        private void RemovePurchase(ref Purchase purchase)
        {
            if (purchase.ZaypayPaymentId == 0)
            {
                db.Purchases.Remove(purchase);
                db.SaveChanges();
            }
        }

        private bool AllValuesPresent(ref NameValueCollection collection)
        {

            if (
                    (collection["status"] != null) &&
                    (collection["price_setting_id"] != null) &&
                    (collection["payment_id"] != null) &&
                    (collection["purchase_id"] != null) &&
                    (collection["payalogue_id"] != null)
                )
                return true;
            else
                return false;

        }

        private string GetUserIP()
        {
            string pattern = @"^((([0-9]{1,2})|(1[0-9]{2,2})|(2[0-4][0-9])|(25[0-5])|\*)\.){3}(([0-9]{1,2})|(1[0-9]{2,2})|(2[0-4][0-9])|(25[0-5])|\*)$";
            Regex check = new Regex(pattern);
            string local = "127.0.0.1";

            string ipList = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (!string.IsNullOrEmpty(ipList) && check.IsMatch(ipList.Split(',')[0], 0))            
                return ipList.Split(',')[0];            

            if (check.IsMatch(Request.ServerVariables["REMOTE_ADDR"], 0))            
                return Request.ServerVariables["REMOTE_ADDR"];
            

            return local;
            
        }



        private List<SelectListItem> GetLanguages(ref List<Hashtable> hash)
        {

            List<SelectListItem> items = new List<SelectListItem>();

            foreach (Hashtable h in (List<Hashtable>)hash)
            {

                items.Add(new SelectListItem
                {
                    Selected = ((String)h["code"] == "en"),
                    Text = (String)h["english-name"],
                    Value = (String)h["code"]

                });

            }
            return items;
        }

        private List<SelectListItem> GetCountries(ref List<Hashtable> hash)
        {

            List<SelectListItem> items = new List<SelectListItem>();

            foreach (Hashtable h in (List<Hashtable>)hash)
            {
                items.Add(new SelectListItem
                {
                    Text = (String)h["name"],
                    Value = (String)h["code"]

                });
            }
            return items;
        }

        private void LogEntry(string mesg)
        {
            logEntry.Message = mesg;
            Logger.Write(logEntry);
        }


    }
}