using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FinancialPortal.Models
{
    public class Budget
    {
        public int BudgetId { get; set; }
        public int HouseHoldId { get; set; }
        public int? CategoryId { get; set; }
        public decimal BudgetAmount { get; set; }
        public System.DateTimeOffset BudgetCrtDate { get; set; }
        public Nullable<System.DateTimeOffset> BudgetUpdDate { get; set; }
        public bool IsWithdrawl { get; set; }
        public string BudgetDescription { get; set; }

        public virtual HouseHold HouseHold { get; set; }
        public virtual Category Category { get; set; }
    }
}