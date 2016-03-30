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
    public class TransactionsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Transactions
        [AuthorizeHouseHoldrequired]
        public ActionResult Index(int? BankAccountId)
        {
            if(!Request.IsAuthenticated)
            {
                return RedirectToAction("Login","Account");
            }

            var bank = db.BankAccount.Find(BankAccountId);
            return View(bank.Transaction.ToList());
        }

        // GET: Transactions/Details/5
        [AuthorizeHouseHoldrequired]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Transaction transaction = db.Transaction.Find(id);
            if (transaction == null)
            {
                return HttpNotFound();
            }
            return View(transaction);
        }

        // GET: Transactions/Create
        [AuthorizeHouseHoldrequired]
        public ActionResult Create(int? bankAccountId)
        {
            var userId = User.Identity.GetUserId();
            var hhId = db.Users.Find(userId).HouseHoldId;
            var bankAccount = db.BankAccount.Where(h => h.HouseHoldId == hhId).ToList();
            var category = db.Category.Where(h => h.HouseHoldId == hhId).ToList();
            
            var transaction = new Transaction();
            transaction.BankAccountId = bankAccountId.Value;
            
            ViewBag.BankAccountId = new SelectList(bankAccount, "BankAccountId", "AccountName",transaction.BankAccountId);
            ViewBag.CategoryId = new SelectList(category, "CategoryId", "CategoryName");
            return View(transaction);
        
        }

        // POST: Transactions/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeHouseHoldrequired]
            public ActionResult Create([Bind(Include = "TransactionId,BankAccountId,CategoryId,IsWithdrawl,TransactionAmount,ReconciliationAmount,TransactionDescription")] Transaction transaction)
        {
            if (ModelState.IsValid)
            {
                var bankAccount = db.BankAccount.Find(transaction.BankAccountId);

                // Prevent Overdraft
                if (bankAccount.Balance < transaction.TransactionAmount)
                {
                    TempData["Overdraft"] = "Insufficient balance in the account. Cannot complete transaction.";
                    return RedirectToAction("Index", new { BankAccountId = transaction.BankAccountId });
                }
                
                transaction.BankUserId = User.Identity.GetUserId();
                transaction.TransactionDate = System.DateTimeOffset.Now;
                transaction.UpdateDate = System.DateTimeOffset.Now;
                transaction.ReconciliationAmount = 0;
                transaction.IsDeleted = false;

                db.Transaction.Add(transaction);

                if(transaction.IsWithdrawl == true)
                {
                    bankAccount.Balance -= transaction.TransactionAmount;
                }
                else
                {
                    bankAccount.Balance += transaction.TransactionAmount;
                }
                
                bankAccount.UpdateDate = System.DateTimeOffset.Now;

                db.SaveChanges();
                return RedirectToAction("Index", new { BankAccountId = transaction.BankAccountId });
            }

            ViewBag.BankAccountId = new SelectList(db.BankAccount, "BankAccountId", "AccountName", transaction.BankAccountId);
            ViewBag.CategoryId = new SelectList(db.Category, "CategoryId", "CategoryName", transaction.CategoryId);
            return View(transaction);
        }

        // GET: Transactions/Edit/5
        [AuthorizeHouseHoldrequired]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Transaction transaction = db.Transaction.Find(id);
            if (transaction == null)
            {
                return HttpNotFound();
            }

            var userId = User.Identity.GetUserId();
            var hhId = db.Users.Find(userId).HouseHoldId;
            var bankAccount = db.BankAccount.Where(h => h.HouseHoldId == hhId).ToList();

            var category = db.Category.Where(h => h.HouseHoldId == hhId).ToList();


            ViewBag.BankAccountId = new SelectList(bankAccount, "BankAccountId", "AccountName", transaction.BankAccountId);
            ViewBag.BankUserId = new SelectList(db.Users, "Id", "FirstName", transaction.BankUserId);
            ViewBag.CategoryId = new SelectList(category, "CategoryId", "CategoryName", transaction.CategoryId);
            return View(transaction);
        }

        // POST: Transactions/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeHouseHoldrequired]
        public ActionResult Edit([Bind(Include = "TransactionId,BankAccountId,CategoryId,IsWithdrawl,TransactionAmount,TransactionDescription,IsDeleted")] Transaction transaction)
        {
            if (ModelState.IsValid)
            {
                db.Transaction.Attach(transaction);
                var oldTransaction = db.Transaction.AsNoTracking().FirstOrDefault(t => t.TransactionId == transaction.TransactionId);
                transaction.BankUserId = User.Identity.GetUserId();
                transaction.UpdateDate = System.DateTimeOffset.Now;
               
                // Find the BankAccountId to be uppdated
                var bankAccount = db.BankAccount.Find(transaction.BankAccountId);
               
                // Check if IsWithdrawl changed
                if(oldTransaction.IsWithdrawl == transaction.IsWithdrawl)
                {
                    var amount = oldTransaction.TransactionAmount - transaction.TransactionAmount;
                    bankAccount.Balance += amount;
                }
                
                if(oldTransaction.IsWithdrawl == true && transaction.IsWithdrawl == false)
                {
                    //get the old balance b4 the old transaction then add the new transaction amount
                    bankAccount.Balance = bankAccount.Balance + oldTransaction.TransactionAmount + transaction.TransactionAmount; 
                }
                
                if(oldTransaction.IsWithdrawl == false && transaction.IsWithdrawl == true)
                {
                    bankAccount.Balance = bankAccount.Balance - oldTransaction.TransactionAmount - transaction.TransactionAmount;
                }
                
                db.Entry(bankAccount).Property("Balance").IsModified = true;
                db.Entry(transaction).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index", new { BankAccountId = transaction.BankAccountId });
            }
            ViewBag.BankAccountId = new SelectList(db.BankAccount, "BankAccountId", "AccountName", transaction.BankAccountId);
            ViewBag.BankUserId = new SelectList(db.Users, "Id", "FirstName", transaction.BankUserId);
            ViewBag.CategoryId = new SelectList(db.Category, "CategoryId", "CategoryName", transaction.CategoryId);
            return View(transaction);
        }

        // GET: Transactions/Delete/5
        [AuthorizeHouseHoldrequired]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Transaction transaction = db.Transaction.Find(id);
            if (transaction == null)
            {
                return HttpNotFound();
            }
            return View(transaction);
        }

        // POST: Transactions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthorizeHouseHoldrequired]
        public ActionResult DeleteConfirmed(int id)
        {
            Transaction transaction = db.Transaction.Find(id);
            transaction.IsDeleted = true;
            
            // If Reconcilaition Transaction then dont make any changes to the Bank Account Balance
            if (!transaction.TransactionDescription.Equals("Reconciliation"))
            {
                var bankAccount = db.BankAccount.Find(transaction.BankAccountId);
                if (transaction.IsWithdrawl)
                {
                    bankAccount.Balance += transaction.TransactionAmount;
                }
                else
                {
                    bankAccount.Balance -= transaction.TransactionAmount;
                }
            }

            db.SaveChanges();
            return RedirectToAction("Index", new { BankAccountId = transaction.BankAccountId });
        }

        public PartialViewResult _IncomeExpense()
        {
            var userId = User.Identity.GetUserId();
            var hhId = db.Users.Find(userId).HouseHoldId;
            decimal income = 0;            
            var bankAccount = db.BankAccount.Where(h => h.HouseHoldId == hhId).ToList();

            decimal expense = 0;
            decimal balance = 0;
            IncomeExpenseViewModel ieVM = new IncomeExpenseViewModel();
            
            foreach(var account in bankAccount)
            {
                var transaction = account.Transaction.ToList();
                var inc = transaction.Where(t => t.IsWithdrawl == false).Where(d => d.IsDeleted == false).ToList();
                var exp = transaction.Where(t => t.IsWithdrawl == true).Where(d => d.IsDeleted == false).ToList();
                
                foreach(var i in inc)
                {
                    income += i.TransactionAmount;
                }

                foreach (var e in exp)
                {
                    expense += e.TransactionAmount; 
                }
                balance = income - expense;
                ieVM.HouseHoldName = account.HouseHold.HouseHoldName;
            }
                        
            ieVM.income = income;
            ieVM.expense = expense;
            ieVM.balance = balance;
            ieVM.Date = System.DateTimeOffset.Now;

            return PartialView(ieVM);
        }

        public PartialViewResult _TransByCategory(int id)
        {
            TransByCategoryViewModel tcVM = new TransByCategoryViewModel();
            var trans = db.Transaction.Where(b => b.BankAccountId == id).ToList();

            var userId = User.Identity.GetUserId();
            var hhId = db.Users.Find(userId).HouseHoldId;

            tcVM.category = db.Category.Where(h => h.HouseHoldId == hhId).ToList();
            tcVM.transaction = trans;
          
            return PartialView(tcVM);
        }

        public PartialViewResult _TransByIncExp(int id)
        {
            var transaction = db.Transaction.Where(b => b.BankAccountId == id).Where(w => w.IsDeleted == false).ToList();
            return PartialView(transaction);
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
