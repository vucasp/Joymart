using System.Web.Mvc;

namespace Joymart.Controllers
{
    public class ErrorController : Controller
    {
        // GET: /Error/Forbidden (Lỗi 403)
        public ActionResult Forbidden()
        {
            Response.StatusCode = 403;
            return View();
        }

        // GET: /Error/NotFound (Lỗi 404)
        public ActionResult NotFound()
        {
            Response.StatusCode = 404;
            return View();
        }
    }
}