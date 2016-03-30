using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FinancialPortal.Models
{
    public class TransByCategoryViewModel
    {
        public List<Category> category = new List<Category>();
        public List<Transaction> transaction = new List<Transaction>();
    }
}