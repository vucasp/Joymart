using Joymart.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Joymart.Filters
{
    public class AdminAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext.Session["UserID"] == null)
            {
                return false;
            }

            int userId = Convert.ToInt32(httpContext.Session["UserID"]);
            using (var db = new DBJoymartEntities())
            {
                var account = db.UserAccounts
                    .Include(u => u.Role)
                    .FirstOrDefault(u => u.UserID == userId);

                if (account == null || !account.IsActive)
                {
                    httpContext.Session.Remove("UserID");
                    httpContext.Session.Remove("UserName");
                    httpContext.Session.Remove("Role");
                    httpContext.Session.Remove("CustomerID");
                    httpContext.Session.Remove("FullName");
                    return false;
                }

                httpContext.Session["Role"] = account.Role.RoleName;
                return account.Role.RoleName == "Admin";
            }
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            // Chưa đăng nhập (hoặc tài khoản vừa bị khóa) -> chuyển về trang Login kèm returnUrl
            if (filterContext.HttpContext.Session["UserID"] == null)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new
                    {
                        controller = "Account",
                        action = "Login",
                        returnUrl = filterContext.HttpContext.Request.RawUrl
                    }));
                return;
            }

            // Đã đăng nhập nhưng không phải Admin -> trả về HTTP 403 Forbidden
            filterContext.Result = new RedirectToRouteResult(
                new RouteValueDictionary(new
                {
                    controller = "Error",
                    action = "Forbidden"
                }));
        }
    }
}
