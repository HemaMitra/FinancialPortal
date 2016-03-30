using FinancialPortal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.Owin.Security;
using Microsoft.AspNet.Identity.Owin;

namespace FinancialPortal.Controllers
{
        // Additional extension class for the newly added HouseHoldId Claim in the Cookie
    public static class Extensions
    {
        public static string GetHouseHoldId(this IIdentity user)
        {
            var claimsIdentity = (ClaimsIdentity)user;
            var HouseHoldClaim = claimsIdentity.Claims.FirstOrDefault(c => c.Type == "HouseHoldId");

            if (HouseHoldClaim != null)
            {
                return HouseHoldClaim.Value;
            }
            else
                return null;
        }

        public static bool IsInHouseHold(this IIdentity user)
        {
            var cUser = (ClaimsIdentity)user;
            var hid = cUser.Claims.FirstOrDefault(c => c.Type == "HouseHoldId");
            return (hid != null && !string.IsNullOrWhiteSpace(hid.Value));
        }

        public static async Task RefreshAuthentication(this HttpContextBase context, ApplicationUser user)
        {
            context.GetOwinContext().Authentication.SignOut();
            await context.GetOwinContext().Get<ApplicationSignInManager>().SignInAsync(user, isPersistent: false, rememberBrowser: false);
        }
    }

    public class AuthorizeHouseHoldrequired : AuthorizeAttribute 
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var isAuthorized = base.AuthorizeCore(httpContext);

            if(!isAuthorized)
            {
                return false;
            }
            return httpContext.User.Identity.IsInHouseHold();
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            
            if(!filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                base.HandleUnauthorizedRequest(filterContext);
            }
            else
            {
                filterContext.Result = new RedirectToRouteResult(new
                    RouteValueDictionary(new { controller = "HouseHolds", action = "Create" }));
            }
        }



    }
}