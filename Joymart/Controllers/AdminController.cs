using Joymart.Filters;
using Joymart.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace Joymart.Controllers
{
    // =====================================================================
    // CONTROLLER TRANG TỔNG QUAN QUẢN TRỊ (AdminController)
    // Nhiệm vụ:
    //   - Tự động đồng bộ hoàn thành các đơn đang giao quá 7 ngày.
    //   - Tổng hợp các chỉ số KPI toàn hệ thống (Doanh thu, Đơn hàng, Kho, Tài khoản).
    //   - Thống kê tỷ lệ trạng thái đơn hàng (biểu đồ cột CSS), Top 5 sản phẩm bán chạy,
    //     danh sách sản phẩm sắp hết kho (<= 5) và 5 đơn hàng gần nhất.
    // =====================================================================
    [AdminAuthorize]
    public class AdminController : Controller
    {
        private DBJoymartEntities db = new DBJoymartEntities();

        // GET: /Admin
        public ActionResult Index()
        {
            OrderWorkflowHelper.SyncAutoCompletedOrders(db);

            int totalOrders = db.OrderProes.Count();
            string[] allStatuses = { "Chờ xử lý", "Đã xác nhận", "Đang giao", "Hoàn thành", "Đã hủy" };
            var statusCounts = db.OrderProes
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionary(x => x.Status, x => x.Count);

            var breakdown = new List<OrderStatusStatItem>();
            foreach (string st in allStatuses)
            {
                int count = statusCounts.ContainsKey(st) ? statusCounts[st] : 0;
                int pct = totalOrders > 0 ? (int)Math.Round((double)count * 100.0 / totalOrders) : 0;
                breakdown.Add(new OrderStatusStatItem
                {
                    StatusName = st,
                    OrderCount = count,
                    Percentage = pct
                });
            }

            // Tính Top 5 sản phẩm bán chạy nhất dựa trên tổng Quantity của các đơn không bị hủy
            var topSelling = db.OrderDetails
                .Where(d => d.OrderPro.Status != "Đã hủy")
                .GroupBy(d => new
                {
                    d.ProductID,
                    d.Product.ProductName,
                    CategoryName = d.Product.Category.CategoryName,
                    d.Product.ImageUrl,
                    d.Product.StockQuantity
                })
                .Select(g => new TopSellingProductItem
                {
                    ProductID = g.Key.ProductID,
                    ProductName = g.Key.ProductName,
                    CategoryName = g.Key.CategoryName,
                    ImageUrl = g.Key.ImageUrl,
                    StockQuantity = g.Key.StockQuantity,
                    TotalSold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .OrderByDescending(x => x.TotalSold)
                .ThenByDescending(x => x.TotalRevenue)
                .Take(5)
                .ToList();

            // Cảnh báo các sản phẩm có tồn kho thấp (<= 5)
            var lowStockProducts = db.Products
                .Include(p => p.Category)
                .Where(p => p.StockQuantity <= 5)
                .OrderBy(p => p.StockQuantity)
                .Take(6)
                .ToList();

            var model = new AdminDashboardViewModel
            {
                TotalProducts = db.Products.Count(),
                ActiveProducts = db.Products.Count(p => p.IsActive),
                TotalCategories = db.Categories.Count(),
                TotalStockUnits = db.Products.Select(p => (int?)p.StockQuantity).Sum() ?? 0,
                LowStockCount = db.Products.Count(p => p.StockQuantity <= 5),
                TotalOrders = totalOrders,
                PendingOrders = db.OrderProes.Count(o => o.Status == "Chờ xử lý" || o.Status == "Đã xác nhận"),
                DeliveringOrders = db.OrderProes.Count(o => o.Status == "Đang giao"),
                TotalRevenue = db.OrderProes
                    .Where(o => o.Status != "Đã hủy")
                    .Select(o => (decimal?)o.TotalAmount)
                    .Sum() ?? 0m,
                TotalUsers = db.UserAccounts.Count(),
                AdminCount = db.UserAccounts.Count(u => u.Role.RoleName == "Admin"),
                CustomerCount = db.UserAccounts.Count(u => u.Role.RoleName == "Customer"),
                StatusBreakdown = breakdown,
                TopSellingProducts = topSelling,
                LowStockProducts = lowStockProducts,
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
