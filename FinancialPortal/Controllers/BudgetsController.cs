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
    public class BudgetsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Budgets
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
            var budget = db.Budget.Where(h => h.HouseHoldId == hhid).ToList();
            
            return View(budget.ToList());
            }
        }

        // GET: Budgets/Details/5
        [AuthorizeHouseHoldrequired]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Budget budget = db.Budget.Find(id);
            if (budget == null)
            {
                return HttpNotFound();
            }
            return View(budget);
        }

        // GET: Budgets/Create
        [AuthorizeHouseHoldrequired]
        public ActionResult Create()
        {
            var userId = User.Identity.GetUserId();
            var hhid = db.Users.Find(userId).HouseHoldId;
            var household = db.HouseHold.ToList();
            var household1 = household.Where(m => m.HouseHoldId == hhid);
            var category = db.Category.Where(c => c.HouseHoldId == hhid).ToList();

            ViewBag.CategoryId = new SelectList(category, "CategoryId", "CategoryName");
            ViewBag.HouseHoldId = new SelectList(household1, "HouseHoldId", "HouseHoldName");
            return View();
        }

        // POST: Budgets/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeHouseHoldrequired]
        public ActionResult Create([Bind(Include = "BudgetId,HouseHoldId,CategoryId,BudgetAmount,Iswithdrawl,BudgetDescription")] Budget budget)
        {
            if (ModelState.IsValid)
            {
                budget.BudgetCrtDate = System.DateTimeOffset.Now;
                budget.BudgetUpdDate = System.DateTimeOffset.Now;

                db.Budget.Add(budget);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.CategoryId = new SelectList(db.Category, "CategoryId", "CategoryName", budget.CategoryId);
            ViewBag.HouseHoldId = new SelectList(db.HouseHold, "HouseHoldId", "HouseHoldName", budget.HouseHoldId);
            return View(budget);
        }

        // GET: Budgets/Edit/5
        [AuthorizeHouseHoldrequired]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Budget budget = db.Budget.Find(id);
            if (budget == null)
            {
                return HttpNotFound();
            }
            var userId = User.Identity.GetUserId();
            var hhid = db.Users.Find(userId).HouseHoldId;
            var household = db.HouseHold.ToList();
            var household1 = household.Where(m => m.HouseHoldId == hhid);

            var category = db.Category.Where(h => h.HouseHoldId == hhid).ToList();


            ViewBag.CategoryId = new SelectList(category, "CategoryId", "CategoryName", budget.CategoryId);
            ViewBag.HouseHoldId = new SelectList(household1, "HouseHoldId", "HouseHoldName", budget.HouseHoldId);
            return View(budget);
        }

        // POST: Budgets/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeHouseHoldrequired]
        public ActionResult Edit([Bind(Include = "BudgetId,HouseHoldId,CategoryId,BudgetAmount,BudgetCrtDate,BudgetUpdDate,IsWithdrawl")] Budget budget)
        {
            if (ModelState.IsValid)
            {
                budget.BudgetUpdDate = System.DateTimeOffset.Now;
                db.Entry(budget).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.CategoryId = new SelectList(db.Category, "CategoryId", "CategoryName", budget.CategoryId);
            ViewBag.HouseHoldId = new SelectList(db.HouseHold, "HouseHoldId", "HouseHoldName", budget.HouseHoldId);
            return View(budget);
        }

        // GET: Budgets/Delete/5
        [AuthorizeHouseHoldrequired]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Budget budget = db.Budget.Find(id);
            if (budget == null)
            {
                return HttpNotFound();
            }
            return View(budget);
        }

        // POST: Budgets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthorizeHouseHoldrequired]
        public ActionResult DeleteConfirmed(int id)
        {
            Budget budget = db.Budget.Find(id);
            db.Budget.Remove(budget);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public PartialViewResult _BudgetIncomeExpense()
        {
            IncomeExpenseViewModel ieVM = new IncomeExpenseViewModel();
            var userId = User.Identity.GetUserId();
            var hhid = db.Users.Find(userId).HouseHoldId;

            var budget = db.Budget.Where(h => h.HouseHoldId == hhid).ToList();
            
            decimal inc = budget.Where(i => i.IsWithdrawl == false).Sum(b => b.BudgetAmount);
            decimal exp = budget.Where(i => i.IsWithdrawl == true).Sum(b => b.BudgetAmount);
            ieVM.HouseHoldName = db.HouseHold.Find(hhid).HouseHoldName;

            ieVM.balance = inc - exp;
            ieVM.income = inc;
            ieVM.expense = exp;
            ieVM.Date = System.DateTimeOffset.Now;
            
            return PartialView(ieVM);
        }

        // Monthly Categorised Actual Vs Budgeted Table
        [AuthorizeHouseHoldrequired]
        public PartialViewResult _MonthlyCatTable()
        {
            var user = db.Users.Find(User.Identity.GetUserId());
            var date = System.DateTimeOffset.Now;
            var chart1 = new List<Object>();

            // Get categories for the current HouseHold
            var category = db.Category.Where(h => h.HouseHoldId == user.HouseHoldId).ToList();

            // Get Budgeted amount for the current month and year for the current Household
            var budgetExp = db.Budget.Where(h => h.HouseHoldId == user.HouseHoldId).Where(d => d.BudgetCrtDate.Year == date.Year).Where(m => m.BudgetCrtDate.Month == date.Month).Where(w => w.IsWithdrawl == true).ToList();
            
            // Get Bank Accounts linked to the Household
            var bankAccounts = db.BankAccount.Where(h => h.HouseHoldId == user.HouseHoldId).ToList();
            var transactionsExp = new List<Transaction>();
            // Get Transactions for all the accounts linked to the household
            foreach (var item in bankAccounts)
            {
                transactionsExp.AddRange(db.Transaction.Where(b => b.BankAccountId == item.BankAccountId).Where(y => y.TransactionDate.Year == date.Year).Where(m => m.TransactionDate.Month == date.Month).Where(w => w.IsWithdrawl == true).Where(d => d.IsDeleted == false).ToList());
            }

           
            var actualVcBudgetedViewModel = new List<ActualVcBudgetedViewModel>();

            // Add to chart
            foreach (var item in category)
            {
                if (item.CategoryName != "Salary")
                {
                    ActualVcBudgetedViewModel avbVM = new ActualVcBudgetedViewModel();    
                    avbVM.Category = item.CategoryName;
                    avbVM.ActualExp = transactionsExp.Where(c => c.CategoryId == item.CategoryId).Sum(a => a.TransactionAmount);
                    avbVM.BudgetExp = budgetExp.Where(c => c.CategoryId == item.CategoryId).Sum(a => a.BudgetAmount);
                    actualVcBudgetedViewModel.Add(avbVM);
                }
            }
            return PartialView(actualVcBudgetedViewModel);
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
