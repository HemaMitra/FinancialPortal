using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FinancialPortal.Models
{
    public class IncomeExpenseViewModel
    {
        public decimal balance { get; set; }
        public string HouseHoldName { get; set; }
        public decimal income { get; set; }
        public decimal expense { get; set; }
        public System.DateTimeOffset Date { get; set; }
    }
}