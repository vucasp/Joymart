using System;
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

            string role = Convert.ToString(httpContext.Session["Role"]);
            return role == "Admin";
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            // Chưa đăng nhập -> chuyển về trang Login kèm returnUrl
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