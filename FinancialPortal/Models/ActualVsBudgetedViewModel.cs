using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FinancialPortal.Models
{
    public class ActualVcBudgetedViewModel
    {
        public string Category { get; set; }
        public decimal ActualInc { get; set; }
        public decimal ActualExp { get; set; }
        public decimal BudgetInc { get; set; }
        public decimal BudgetExp { get; set; }
    }
}