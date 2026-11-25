using Joymart.Filters;
using Joymart.Models;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace Joymart.Controllers
{
    [AdminAuthorize]
    public class AdminController : Controller
    {
        private DBJoymartEntities db = new DBJoymartEntities();

        // GET: /Admin
        public ActionResult Index()
        {
            var model = new AdminDashboardViewModel
            {
                TotalProducts = db.Products.Count(),
                ActiveProducts = db.Products.Count(p => p.IsActive),
                TotalCategories = db.Categories.Count(),
                TotalOrders = db.OrderProes.Count(),
                PendingOrders = db.OrderProes.Count(o => o.Status == "Chờ xử lý"),
                TotalRevenue = db.OrderProes
                    .Where(o => o.Status != "Đã hủy")
                    .Select(o => (decimal?)o.TotalAmount)
                    .Sum() ?? 0m,
                TotalUsers = db.UserAccounts.Count(),
                AdminCount = db.UserAccounts.Count(u => u.Role.RoleName == "Admin"),
                CustomerCount = db.UserAccounts.Count(u => u.Role.RoleName == "Customer"),
                RecentOrders = db.OrderProes
                    .Include(o => o.Customer)
                    .OrderByDescending(o => o.OrderID)
                    .Take(5)
                    .ToList()
            };

            return View(model);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
