using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FinancialPortal.Models
{
    public class BankAccount
    {
        public BankAccount()
        { 
            this.Transaction = new HashSet<Transaction>();
        }

        public int BankAccountId { get; set; }
        [Display(Name = "Account Name")]
        public string AccountName { get; set; }
        [Display(Name = "HouseHold Id")]
        public int? HouseHoldId { get; set; }
        //[Display(Name = "Transaction Id")]
        //public int? TransactionId { get; set; }

        public decimal Balance { get; set; }
        [Display(Name = "Creation Date")]
        //public Nullable<System.DateTimeOffset> CreatedDate { get; set; }
        public System.DateTimeOffset CreatedDate { get; set; }
        [Display(Name = "Update Date")]
        public Nullable<System.DateTimeOffset> UpdateDate { get; set; }
        public string UserId { get; set; }

        public virtual HouseHold HouseHold { get; set; }
        public virtual ICollection<Transaction> Transaction { get; set; }

    }
}