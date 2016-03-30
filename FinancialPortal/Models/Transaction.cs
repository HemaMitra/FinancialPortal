using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FinancialPortal.Models
{
    public class Transaction
    {

        public int TransactionId { get; set; }

        [Display(Name = "Bank Account Id")]
        public int BankAccountId { get; set; }
        
        [Display(Name = "Bank Account User")]
        public string BankUserId { get; set; }

        public int? CategoryId { get; set; }
        public bool IsWithdrawl { get; set; }
        
        [Display(Name = "Transaction Amount")]
        public decimal TransactionAmount { get; set; }
        
        [Display(Name = "Reconciciation Amount")]
        public decimal ReconciliationAmount { get; set; }

        [Display(Name = "Transaction Date")]
        public System.DateTimeOffset TransactionDate { get; set; }

        [Display(Name = "Update Date")]
        public Nullable<System.DateTimeOffset> UpdateDate { get; set; }

        [Display(Name = "Transaction Description")]
        public string TransactionDescription { get; set; }
        public bool IsDeleted { get; set; }

        public virtual BankAccount BankAccount { get; set; }
        public virtual ApplicationUser BankUser { get; set; }
        
        public virtual Category Category { get; set; }

    }
}