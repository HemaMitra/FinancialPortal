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
using System.Configuration;
using System.Threading.Tasks;
using FinancialPortal.Controllers;
using Newtonsoft.Json;

namespace FinancialPortal.Controllers
{
    public class HouseHoldsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: HouseHolds
        [AuthorizeHouseHoldrequired]
        public ActionResult Index()
        {
            if(!Request.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            var user = db.Users.Find(User.Identity.GetUserId());
            var hhid = user.HouseHoldId;
            
            if(hhid == null)
            {
                TempData["Message"] = "You are not a part of any household. Please create one to proceed." ;
                return RedirectToAction("Create");
            }
            
            var houseHold = db.HouseHold.Find(hhid);
            return View(houseHold);
        }

        [AuthorizeHouseHoldrequired]
        public ActionResult Details()
        {
            if (!Request.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = db.Users.Find(User.Identity.GetUserId());
            var hhid = user.HouseHoldId;

            HouseHold houseHold = db.HouseHold.Find(hhid);
            if (houseHold == null)
            {
                //return HttpNotFound();
                return RedirectToAction("Create","HouseHolds");
            }
            return View(houseHold);
        }



        // GET: HouseHolds/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: HouseHolds/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "HouseHoldId,HouseHoldName,CreatedDate")] HouseHold houseHold)
        {
            if (ModelState.IsValid)
            {
                var user = db.Users.Find(User.Identity.GetUserId());
                houseHold.CreatedDate = System.DateTimeOffset.Now;
                db.HouseHold.Add(houseHold);
                
                // Update HouseHoldID for the user in the User Table
                user.HouseHoldId = houseHold.HouseHoldId;
                db.SaveChanges();
                
                // Add Default Categories to the new household
                
                var hName = db.HouseHold.Where(hn => hn.HouseHoldName == houseHold.HouseHoldName).FirstOrDefault();
                var hhid = hName.HouseHoldId;

                HouseHoldsController.DefaultCategories("Food", hhid);
                HouseHoldsController.DefaultCategories("Salary", hhid);
                HouseHoldsController.DefaultCategories("Gas", hhid);
                HouseHoldsController.DefaultCategories("Mortgage", hhid);
                HouseHoldsController.DefaultCategories("Miscellaneous", hhid);
                
                // Signoff and signin the user
                await ControllerContext.HttpContext.RefreshAuthentication(user);
                return RedirectToAction("Details", new { id = houseHold.HouseHoldId });
            }

            return View(houseHold);
        }

        // GET: HouseHolds/Edit/5
        [AuthorizeHouseHoldrequired]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            HouseHold houseHold = db.HouseHold.Find(id);
            if (houseHold == null)
            {
                return HttpNotFound();
            }
            return View(houseHold);
        }

        // POST: HouseHolds/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [AuthorizeHouseHoldrequired]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "HouseHoldId,HouseHoldName,CreatedDate")] HouseHold houseHold)
        {
            if (ModelState.IsValid)
            {
                HouseHold h = db.HouseHold.Find(houseHold.HouseHoldId);
                h.HouseHoldName = houseHold.HouseHoldName;
                db.HouseHold.Attach(h);
                db.Entry(h).Property("HouseHoldName").IsModified = true;
       
                db.SaveChanges();
                return RedirectToAction("Details", new { id = houseHold.HouseHoldId });
            }
            return View(houseHold);
        }

        // GET: HouseHolds/Delete/5
        [AuthorizeHouseHoldrequired]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            HouseHold houseHold = db.HouseHold.Find(id);
            if (houseHold == null)
            {
                return HttpNotFound();
            }
            return View(houseHold);

        }

        // POST: HouseHolds/Delete/5
        [HttpPost, ActionName("Delete")]
        [AuthorizeHouseHoldrequired]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            HouseHold houseHold = db.HouseHold.Find(id);

            // Make HHID null in Users table
            var user = db.Users.Find(User.Identity.GetUserId());
            user.HouseHoldId = null;
            
            // Remove Budgets
            var budget = db.Budget.Where(h => h.HouseHoldId == houseHold.HouseHoldId).ToList();
            if(budget != null)
            {
                db.Budget.RemoveRange(budget);
            }
            
            // Remove Categories
            var category = db.Category.Where(h => h.HouseHoldId == houseHold.HouseHoldId).ToList();
            db.Category.RemoveRange(category);

            // Remove BankAccount Attached to the HouseHold
            var bankAccount = db.BankAccount.Where(h => h.HouseHoldId == houseHold.HouseHoldId).ToList();
            if(bankAccount != null)
            { 
                db.BankAccount.RemoveRange(bankAccount);
            }
            // Remove HouseHold
            db.HouseHold.Remove(houseHold);
            
            db.SaveChanges();
            return RedirectToAction("Create");
        }

        [AuthorizeHouseHoldrequired]
        public ActionResult Invite([Bind(Include = "HouseHoldId,MemberEmail")] Member member)
        {
            // get Random Code
            int length = 5;
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789#%$_";
            var random = new Random();
            string code = new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
            
            member.InvitationCode = code;
            member.IsMember = false;
            db.Member.Add(member);
            db.SaveChanges();

            //Get HouseHold Name
            var hh = db.HouseHold.Find(member.HouseHoldId);

            // Send Invitation
            var callbackUrl = Url.Action("Login", "Account", null, protocol: Request.Url.Scheme);
            IdentityMessage msg = new IdentityMessage();
            EmailService ems = new EmailService();

            msg.Subject = "You have been invited to join my household";
            msg.Destination = member.MemberEmail;
            
            msg.Body = "You are invited to join <strong>" +hh.HouseHoldName + "</strong> HouseHold in the MItra's financial portal.<BR>Please join by entering the code : " + code + ".Please join by clicking <a href=\"" + callbackUrl + "\">here</a>";

            ems.SendAsync(msg);

            return RedirectToAction("Details", new { id = member.HouseHoldId});
        }

        // Join HouseHold
        [Authorize]
        public async Task<ActionResult> JoinHouseHold(string JMemberEmail, string JInvitationCode)
        {
            if (ModelState.IsValid)
            {
                var member = db.Member.Where(m => m.MemberEmail == JMemberEmail.Trim()).FirstOrDefault();
                
                if(member == null)
                {
                    TempData["Message"] = "No invitation found for " + JMemberEmail + ".Please create a household to proceed."; ;
                    return RedirectToAction("Create","HouseHolds");
                }
                var HouseHoldId = member.HouseHoldId;
        
                if (member.InvitationCode == JInvitationCode.Trim())
                {
                    var user = db.Users.Where(u => u.UserName == JMemberEmail).FirstOrDefault();
                    
                    // Check if user is already a mamber of any household
                    if(user.HouseHoldId != null)
                    {
                        TempData["Message"] = "You are already a member of one househould. Please leave your current household to join a new one.";
                        return RedirectToAction("Details", new { id = user.HouseHoldId });
                    }
                    user.HouseHoldId = member.HouseHoldId;

                    db.Member.Remove(member);

                    db.SaveChanges();
                    user = db.Users.Find(User.Identity.GetUserId());
                    await ControllerContext.HttpContext.RefreshAuthentication(user);
                    TempData["Message"] = "You are added to the household.";
                }
                return RedirectToAction("Details", new { id = HouseHoldId });
            }
            return RedirectToAction("Create", "HouseHolds");
        }

        [AuthorizeHouseHoldrequired]
        public PartialViewResult _ViewMembers(int? id)
        {
            var users = db.Users.Where(u => u.HouseHoldId == id).ToList();
            TempData["id"] = id;
            return PartialView(users.ToList()); 
        }

        [AuthorizeHouseHoldrequired]
        public PartialViewResult _ViewAccounts(int id)
        {
            var bankAccount = db.BankAccount.Where(hh => hh.HouseHoldId == id).ToList();
            TempData["id"] = id;
            return PartialView(bankAccount);
        }

        [AuthorizeHouseHoldrequired]
        public async Task<ActionResult> LeaveHouseHold(int? HouseHoldId)
        {
            Console.Write("In LeaveHouseHold");
            if(ModelState.IsValid)
            {
                // Check if last member of household
                var member = db.Users.Where(h => h.HouseHoldId == HouseHoldId).Count();

                if(member > 1)
                { 
                    var user = db.Users.Find(User.Identity.GetUserId());
                    user.HouseHoldId = null;
                
                    db.SaveChanges();
                    // Calling Referesh to silently signoff and sign the user back in so that the changes will be reflected in the database.
                    await ControllerContext.HttpContext.RefreshAuthentication(user);
                }
                else
                {
                    return RedirectToAction("Delete", new { id = HouseHoldId });
                }
            }
            return RedirectToAction("Create");
        }

        [AuthorizeHouseHoldrequired]
        public ActionResult Charts()
        {
            if(!Request.IsAuthenticated)
            {
                return RedirectToAction("Login","Accounts");
            }
            var user = db.Users.Find(User.Identity.GetUserId());
            var hhid = user.HouseHoldId;
            if (hhid == null)
            {
                TempData["Message"] = "You are not a part of any household. Please create one to proceed.";
                return RedirectToAction("Create", "HouseHolds");
            }
            return View();
        }
        
        // Get the actual expense information for the chart
        [AuthorizeHouseHoldrequired]
        public ActionResult GetChart()
        {
            var user = db.Users.Find(User.Identity.GetUserId());
            var year = System.DateTimeOffset.Now.Year;
            var bankAccounts = db.BankAccount.Where(h => h.HouseHoldId == user.HouseHoldId).ToList();
            var category = db.Category.Where(h => h.HouseHoldId == user.HouseHoldId).ToList();
            var transactions = new List<Transaction>();

            // Get Transactions for all the accounts linked to the household
            foreach(var item in bankAccounts)
            {
                transactions.AddRange(db.Transaction.Where(b => b.BankAccountId == item.BankAccountId).Where(d => d.TransactionDate.Year == year).Where(w => w.IsWithdrawl == true).Where(i => i.IsDeleted == false).ToList());
            }
            
            var chart1 = new List<Object>();
            
            foreach(var item in category)
            {   
                if(item.CategoryName != "Salary")
                { 
                    var trans = transactions.Where(c => c.CategoryId == item.CategoryId).ToList();
                    chart1.Add(new { label = item.CategoryName, value = transactions.Where(c => c.CategoryId == item.CategoryId).Sum(a => a.TransactionAmount) });
                }
            }
            // Convert list to array as JSON needs an array for displaying charts
            var chart = chart1.ToArray();

            return Content(JsonConvert.SerializeObject(chart),"application/json");
        }

        // Get the budgeted expense for the chart
        [AuthorizeHouseHoldrequired]
        public ActionResult GetBudgetedChart()
        {
            var user = db.Users.Find(User.Identity.GetUserId());
            var year = System.DateTimeOffset.Now.Year;

            var budget = db.Budget.Where(h => h.HouseHoldId == user.HouseHoldId).Where(d => d.BudgetCrtDate.Year == year).Where(w => w.IsWithdrawl == true).ToList();
            var category = db.Category.Where(h => h.HouseHoldId == user.HouseHoldId).ToList();

            var chart1 = new List<Object>();
            foreach (var item in category)
            {
                if (item.CategoryName != "Salary")
                {
                    chart1.Add(new { label = item.CategoryName, value = budget.Where(c => c.CategoryId == item.CategoryId).Sum(a => a.BudgetAmount) });
                }
            }
            var chart = chart1.ToArray();
            return Content(JsonConvert.SerializeObject(chart), "application/json");
        }

        //Get Monthly expense for chart
        [AuthorizeHouseHoldrequired]
        public ActionResult GetMonthlyChart()
        {
            var user = db.Users.Find(User.Identity.GetUserId());
            var date = System.DateTimeOffset.Now;
            var year = date.Year;
            var chart1 = new List<Object>();
            string[] months = new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            decimal[] monthlyBudget = new decimal[12];
            decimal[] monthlyActual = new decimal[12];

            // Get budget for the household for the current year
            var budget = db.Budget.Where(h => h.HouseHoldId == user.HouseHoldId).Where(y => y.BudgetCrtDate.Year == year).Where(w => w.IsWithdrawl == true).ToList();
            
            // add budget expense
            for (int i = 0; i < 12; i++)
            {
                monthlyBudget[i] = budget.Where(m => m.BudgetCrtDate.Month == i + 1).Sum(a => a.BudgetAmount);
            }

            // Get Bank Accounts linked to the Household
            var bankAccounts = db.BankAccount.Where(h => h.HouseHoldId == user.HouseHoldId).ToList();
            var transactions = new List<Transaction>();

            // Get Transactions for all the accounts linked to the household
            foreach (var item in bankAccounts)
            {
                transactions.AddRange(db.Transaction.Where(b => b.BankAccountId == item.BankAccountId).Where(y => y.TransactionDate.Year == year).Where(w => w.IsWithdrawl == true).Where(d => d.IsDeleted == false).ToList());
            }

            // Get Actual Expense
            for (int i = 0; i < 12; i++)
            {
                monthlyActual[i] = transactions.Where(m => m.TransactionDate.Month == i+1).Sum(a => a.TransactionAmount);
            }

            // Add to chart
            for(int i = 0;i<12;i++)
            {
                chart1.Add(new { mth = months[i], actual = monthlyActual[i], budget = monthlyBudget[i] });
            }

            // Convet chart to Array
            var chart = chart1.ToArray();
            
            return Content(JsonConvert.SerializeObject(chart), "application/json");
        }

        // Get Monthly Expense Categorised
        [AuthorizeHouseHoldrequired]
        public ActionResult GetMonthlyCat()
        {
            var user = db.Users.Find(User.Identity.GetUserId());
            var date = System.DateTimeOffset.Now;
            var chart1 = new List<Object>();
            
            // Get categories for the current HouseHold
            var category = db.Category.Where(h => h.HouseHoldId == user.HouseHoldId).ToList();
            
            // Get Budgeted amount for the current month and year for the current Household
            var budget = db.Budget.Where(h => h.HouseHoldId == user.HouseHoldId).Where(d => d.BudgetCrtDate.Year == date.Year).Where(m => m.BudgetCrtDate.Month == date.Month).Where(w => w.IsWithdrawl == true).ToList();

            // Get Bank Accounts linked to the Household
            var bankAccounts = db.BankAccount.Where(h => h.HouseHoldId == user.HouseHoldId).ToList();
            var transactions = new List<Transaction>();

            // Get Transactions for all the accounts linked to the household
            foreach (var item in bankAccounts)
            {
                transactions.AddRange(db.Transaction.Where(b => b.BankAccountId == item.BankAccountId).Where(y => y.TransactionDate.Year == date.Year).Where(m => m.TransactionDate.Month == date.Month).Where(w => w.IsWithdrawl == true).Where(d => d.IsDeleted == false).ToList());
            }

            // Add to chart
            foreach (var item in category)
            {
                if (item.CategoryName != "Salary")
                {
                    chart1.Add(new { cat = item.CategoryName, actual = transactions.Where(c => c.CategoryId == item.CategoryId).Sum(a => a.TransactionAmount), 
                        budget = budget.Where(c => c.CategoryId == item.CategoryId).Sum(a => a.BudgetAmount)});
                    
                }
            }
            // Convet chart to Array
            var chart = chart1.ToArray();
            
            return Content(JsonConvert.SerializeObject(chart), "application/json");
        }
        
        // Get monthly expense actual vs budgeted for table display
        [AuthorizeHouseHoldrequired]
        public PartialViewResult _ViewActualVsBudgeted()
        {
            var user = db.Users.Find(User.Identity.GetUserId());
            var date = System.DateTimeOffset.Now;

            ActualVcBudgetedViewModel abVM = new ActualVcBudgetedViewModel();
            // Get Budgeted amount for the current month and year for the current Household
            abVM.BudgetExp = db.Budget.Where(h => h.HouseHoldId == user.HouseHoldId).Where(d => d.BudgetCrtDate.Year == date.Year).Where(m => m.BudgetCrtDate.Month == date.Month).Where(w => w.IsWithdrawl == true).Sum(b => b.BudgetAmount);
            abVM.BudgetInc = db.Budget.Where(h => h.HouseHoldId == user.HouseHoldId).Where(d => d.BudgetCrtDate.Year == date.Year).Where(m => m.BudgetCrtDate.Month == date.Month).Where(w => w.IsWithdrawl == false).Sum(b => b.BudgetAmount);

            // Get Bank Accounts linked to the Household
            var bankAccounts = db.BankAccount.Where(h => h.HouseHoldId == user.HouseHoldId).ToList();
            var transactionsExp = new List<Transaction>();
            var transactionsInc = new List<Transaction>();

            // Get Transactions for all the accounts linked to the household
            foreach (var item in bankAccounts)
            {
                transactionsExp.AddRange(db.Transaction.Where(b => b.BankAccountId == item.BankAccountId).Where(y => y.TransactionDate.Year == date.Year).Where(m => m.TransactionDate.Month == date.Month).Where(w => w.IsWithdrawl == true).Where(d => d.IsDeleted == false).ToList());
                transactionsInc.AddRange(db.Transaction.Where(b => b.BankAccountId == item.BankAccountId).Where(y => y.TransactionDate.Year == date.Year).Where(m => m.TransactionDate.Month == date.Month).Where(w => w.IsWithdrawl == false).Where(d => d.IsDeleted == false).ToList());
            }

            abVM.ActualExp = transactionsExp.Sum(t => t.TransactionAmount);
            abVM.ActualInc = transactionsInc.Sum(t => t.TransactionAmount);
        
            return PartialView(abVM);
        }

        // Set default categories for the new HouseHold
        public static void DefaultCategories(string catName,int hhId)
        {
            Category category = new Category();
            category.CategoryName = catName;
            category.HouseHoldId = hhId;

            ApplicationDbContext db = new ApplicationDbContext();
            db.Category.Add(category);
            db.SaveChanges();
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
