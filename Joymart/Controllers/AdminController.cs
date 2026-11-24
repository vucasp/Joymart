using Joymart.Filters;
using System.Web.Mvc;

namespace Joymart.Controllers
{
    [AdminAuthorize]
    public class AdminController : Controller
    {
        // GET: /Admin
        public ActionResult Index()
        {
            return View();
        }
    }
}