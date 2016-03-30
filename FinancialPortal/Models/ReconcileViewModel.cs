using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FinancialPortal.Models
{
    public class ReconcileViewModel
    {
        public int bankAccountId { get; set; }
        public decimal reconcileAmount { get; set; }
    }
}