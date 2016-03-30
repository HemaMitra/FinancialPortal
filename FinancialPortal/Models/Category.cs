using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FinancialPortal.Models
{
    public class Category
    {
        public Category()
        {
            this.Transaction = new HashSet<Transaction>();
        }

        public int CategoryId { get;set; }
        public string CategoryName { get; set; }
        public int? HouseHoldId { get; set; }

        public virtual ICollection<Transaction> Transaction { get; set; }
        public virtual HouseHold HouseHold { get; set; }
    }
}