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
    public class BankAccountsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: BankAccounts
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
                var bankAccount = db.BankAccount.Where(b => b.HouseHoldId == hhid);
                return View(bankAccount.ToList());
            }
        }

        // GET: BankAccounts/Details/5
        [AuthorizeHouseHoldrequired]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BankAccount bankAccount = db.BankAccount.Find(id);
            if (bankAccount == null)
            {
                return HttpNotFound();
            }
            return View(bankAccount);
        }

        // GET: BankAccounts/Create
        [AuthorizeHouseHoldrequired]
        public ActionResult Create()
        {
            var user = db.Users.Find(User.Identity.GetUserId());
            var hhid = user.HouseHoldId;
            var household = db.HouseHold.ToList();
            var household1 = household.Where(m => m.HouseHoldId == hhid);

            ViewBag.HouseHoldId = new SelectList(household1, "HouseHoldId", "HouseHoldName");
            return View();
        }

        // POST: BankAccounts/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [AuthorizeHouseHoldrequired]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "BankAccountId,AccountName,HouseHoldId,Balance,CreatedDate,UpdateDate")] BankAccount bankAccount)
        {
            if (ModelState.IsValid)
            {
                bankAccount.CreatedDate = System.DateTimeOffset.Now;
                bankAccount.UpdateDate = null;
                db.BankAccount.Add(bankAccount);

                // Get Category Id for Miscellaneous
                var user = db.Users.Find(User.Identity.GetUserId());
                var hhid = user.HouseHoldId;
            
                var category = db.Category.Where(h => h.HouseHoldId == hhid).ToList();
                var catId = category.Where(c => c.CategoryName == "Miscellaneous").FirstOrDefault();
                
                // create a default transaction immediately after account creation
                
                Transaction transaction = new Transaction();

                transaction.BankAccountId = bankAccount.BankAccountId;
                transaction.BankUserId = User.Identity.GetUserId();
                //transaction.CategoryId = 5;
                transaction.CategoryId = catId.CategoryId;

                transaction.ReconciliationAmount = 0;
                transaction.TransactionDate = System.DateTimeOffset.Now;
                transaction.UpdateDate = System.DateTimeOffset.Now;
                transaction.TransactionAmount = bankAccount.Balance;
                transaction.TransactionDescription = "New Account";
                transaction.IsDeleted = false;
                transaction.IsWithdrawl = false;
                db.Transaction.Add(transaction);

                // end create default transaction

                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.HouseHoldId = new SelectList(db.HouseHold, "HouseHoldId", "HouseHoldName", bankAccount.HouseHoldId);
            return View(bankAccount);
        }

        // GET: BankAccounts/Edit/5
        [AuthorizeHouseHoldrequired]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BankAccount bankAccount = db.BankAccount.Find(id);
            if (bankAccount == null)
            {
                return HttpNotFound();
            }

            var userId = User.Identity.GetUserId();
            var hhid = db.Users.Find(userId).HouseHoldId;
            var household = db.HouseHold.ToList();
            var household1 = household.Where(m => m.HouseHoldId == hhid);

            
            ViewBag.HouseHoldId = new SelectList(household1, "HouseHoldId", "HouseHoldName", bankAccount.HouseHoldId);
            return View(bankAccount);
        }

        // POST: BankAccounts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [AuthorizeHouseHoldrequired]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "BankAccountId,AccountName,HouseHoldId,Balance,CreatedDate,UpdateDate")] BankAccount bankAccount)
        {
            if (ModelState.IsValid)
            {
                bankAccount.UpdateDate = System.DateTimeOffset.Now;
                db.Entry(bankAccount).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.HouseHoldId = new SelectList(db.HouseHold, "HouseHoldId", "HouseHoldName", bankAccount.HouseHoldId);
            return View(bankAccount);
        }

        // GET: BankAccounts/Delete/5
        [AuthorizeHouseHoldrequired]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BankAccount bankAccount = db.BankAccount.Find(id);
            if (bankAccount == null)
            {
                return HttpNotFound();
            }
            return View(bankAccount);
        }

        // POST: BankAccounts/Delete/5
        [HttpPost, ActionName("Delete")]
        [AuthorizeHouseHoldrequired]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            BankAccount bankAccount = db.BankAccount.Find(id);
            db.BankAccount.Remove(bankAccount);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [AuthorizeHouseHoldrequired]
        public PartialViewResult _CreateReconcile(int bankAccountId)
        {
            ReconcileViewModel rViweModel = new ReconcileViewModel();
            rViweModel.bankAccountId = bankAccountId;
            rViweModel.reconcileAmount = 0;
            return PartialView(rViweModel);
        }

        [HttpPost]
        [AuthorizeHouseHoldrequired]
        [ValidateAntiForgeryToken]
        public ActionResult CreateReconcileTrans(int bankAccountId,decimal ReconciliationAmount)
        {
            var bankId = db.BankAccount.Find(bankAccountId);
            var balance = bankId.Balance;

            if(balance == ReconciliationAmount)
            {
                TempData["Message"] = "Balance and Reconciliation Amount are same.No transaction created.";

                return RedirectToAction("Details", "BankAccounts", new { id = bankAccountId });
            }

            Transaction transaction = new Transaction();
            
            transaction.BankAccountId = bankAccountId;
            transaction.BankUserId = User.Identity.GetUserId();
            transaction.CategoryId = 5;
            
            transaction.ReconciliationAmount = ReconciliationAmount;
            transaction.TransactionDate = System.DateTimeOffset.Now;
            transaction.UpdateDate = System.DateTimeOffset.Now;
            transaction.TransactionDescription = "Reconciliation";
            transaction.IsDeleted = false;

            if (balance > ReconciliationAmount)
            {
                transaction.TransactionAmount = balance - ReconciliationAmount;
                transaction.IsWithdrawl = true;
                bankId.Balance = balance - transaction.TransactionAmount;
            }
            if (balance < ReconciliationAmount)
            {
                transaction.TransactionAmount = ReconciliationAmount - balance;
                transaction.IsWithdrawl = false;
                bankId.Balance = balance + transaction.TransactionAmount;
            }

            db.Transaction.Add(transaction);
            db.SaveChanges();

            return RedirectToAction("Details", "BankAccounts", new { id = bankAccountId });
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
