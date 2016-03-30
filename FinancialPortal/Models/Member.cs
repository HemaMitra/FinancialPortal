using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FinancialPortal.Models
{
    public class Member
    {
        public int MemberId { get; set; }
        public string MemberEmail { get; set; }
        public string InvitationCode { get; set; }
        public int HouseHoldId { get; set; }
        public bool IsMember { get; set; }

        public virtual HouseHold HouseHold { get; set; }
    }
}