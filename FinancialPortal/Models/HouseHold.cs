using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FinancialPortal.Models
{
    public class HouseHold
    {
        public HouseHold()
        {
            this.BankAccount = new HashSet<BankAccount>();
            this.Category = new HashSet<Category>();
        }
        
        public int HouseHoldId { get; set; }

        [Display(Name = "HouseHold Name")]
        public string HouseHoldName { get; set; }

        [Display(Name = "Created Date")]
        public System.DateTimeOffset CreatedDate { get; set; }

        public virtual ICollection<Budget> Budget { get; set; }
        public virtual ICollection<ApplicationUser> User { get; set; }
        public virtual ICollection<BankAccount> BankAccount { get; set; }
        public virtual ICollection<Category> Category { get; set; }
    }
}