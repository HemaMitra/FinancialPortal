using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FinancialPortal.Models;
using Microsoft.AspNet.Identity;

namespace FinancialPortal.Controllers
{
    public class CategoriesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Categories
        [AuthorizeHouseHoldrequired]
        public ActionResult Index()
        {
            if(!Request.IsAuthenticated)
            {
                return RedirectToAction("Login","Account");
            }

            var user = db.Users.Find(User.Identity.GetUserId());
            var hhid = user.HouseHoldId;
            if (hhid == null)
            {
                TempData["Message"] = "You are not a part of any household. Please create one to proceed.";
                return RedirectToAction("Create", "HouseHolds");
            }
            else
            {
                var category = db.Category.Where(h => h.HouseHoldId == hhid);
                return View(category.ToList());
            }
        }

        // GET: Categories/Details/5
        [AuthorizeHouseHoldrequired]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Category category = db.Category.Find(id);
            if (category == null)
            {
                return HttpNotFound();
            }
            return View(category);
        }

        // GET: Categories/Create
        [AuthorizeHouseHoldrequired]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeHouseHoldrequired]
        public ActionResult Create([Bind(Include = "CategoryId,CategoryName,HouseHoldId")] Category category)
        {
            if (ModelState.IsValid)
            {
                var userId = User.Identity.GetUserId();
                var hhid = db.Users.Find(userId).HouseHoldId;

                category.HouseHoldId = hhid;
                db.Category.Add(category);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(category);
        }

        // GET: Categories/Edit/5
        [AuthorizeHouseHoldrequired]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Category category = db.Category.Find(id);
            if (category == null)
            {
                return HttpNotFound();
            }
            return View(category);
        }

        // POST: Categories/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeHouseHoldrequired]
        public ActionResult Edit([Bind(Include = "CategoryId,CategoryName,HouseHoldId")] Category category)
        {
            if (ModelState.IsValid)
            {
                db.Entry(category).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(category);
        }

        // GET: Categories/Delete/5
        [AuthorizeHouseHoldrequired]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Category category = db.Category.Find(id);
            if (category == null)
            {
                return HttpNotFound();
            }
            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthorizeHouseHoldrequired]
        public ActionResult DeleteConfirmed(int id)
        {
            // Check transactions for the category to be deleted and make them default to Miscellaneous
            var user = db.Users.Find(User.Identity.GetUserId());

            // Find CategoryId for Miscellaneous
            var misCategory = db.Category.AsNoTracking().Where(h => h.HouseHoldId == user.HouseHoldId).ToList();
            var catId = misCategory.Where(c => c.CategoryName == "Miscellaneous").FirstOrDefault();

            if(catId.CategoryId == id)
            {
                TempData["warning"] = "Miscellaneous Category Cannot Be Deleted.";
                return RedirectToAction("Index","Categories");
            }

            var bankAccount = db.BankAccount.Where(h => h.HouseHoldId == user.HouseHoldId).ToList();
            var transactions = new List<Transaction>();
            
            if(bankAccount != null)
            { 
                foreach(var acc in bankAccount)
                {    
                    transactions.AddRange(db.Transaction.Where(b => b.BankAccountId == acc.BankAccountId).Where(c => c.CategoryId == id).ToList());
                }
            }
            // Update CategoryId to Miscellaneous in the transactions with the to be deleted CategoryId.
            if(transactions != null)
            { 
                foreach(var item in transactions)
                {
                    item.CategoryId = catId.CategoryId;
                    item.UpdateDate = System.DateTimeOffset.Now;
                }
            }

            Category category = db.Category.Find(id);
            // Same functionality for budget
            var budget = db.Budget.Where(h => h.HouseHoldId == user.HouseHoldId).Where(c => c.CategoryId == id).ToList();
            if(budget != null)
            {
                foreach (var item in budget)
                {
                    item.CategoryId = catId.CategoryId;
                    item.BudgetDescription = category.CategoryName;
                }
            }

            //Category category = db.Category.Find(id);
            db.Category.Remove(category);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
