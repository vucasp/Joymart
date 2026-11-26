using Joymart.Filters;
using Joymart.Models;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace Joymart.Controllers
{
    // =====================================================================
    // CONTROLLER QUẢN LÝ KHO HÀNG (InventoryController)
    // Cơ chế đồng bộ:
    //   Quản lý kho thao tác trực tiếp trên trường StockQuantity của bảng Product.
    //   Khi nhập thêm hàng mới tại đây, số lượng tồn kho sẽ tự động cộng dồn và
    //   đồng bộ tức thời 100% với số lượng sản phẩm đăng bán trên cửa hàng.
    // =====================================================================
    [AdminAuthorize]
    public class InventoryController : Controller
    {
        private DBJoymartEntities db = new DBJoymartEntities();

        // GET: /Inventory (Xem số lượng hàng còn lại trong kho & lọc tình trạng tồn kho)
        public ActionResult Index(string keyword, int? categoryId, string stockStatus)
        {
            var allProducts = db.Products.Include(p => p.Category).AsQueryable();

            int totalSku = db.Products.Count();
            int totalUnits = db.Products.Select(p => (int?)p.StockQuantity).Sum() ?? 0;
            int lowStockCount = db.Products.Count(p => p.StockQuantity > 0 && p.StockQuantity <= 5);
            int outOfStockCount = db.Products.Count(p => p.StockQuantity <= 0);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                allProducts = allProducts.Where(p => p.ProductName.Contains(keyword));
            }

            if (categoryId.HasValue)
            {
                allProducts = allProducts.Where(p => p.CategoryID == categoryId.Value);
            }

            switch (stockStatus)
            {
                case "low":
                    allProducts = allProducts.Where(p => p.StockQuantity > 0 && p.StockQuantity <= 5);
                    break;
                case "out":
                    allProducts = allProducts.Where(p => p.StockQuantity <= 0);
                    break;
                case "instock":
                    allProducts = allProducts.Where(p => p.StockQuantity > 5);
                    break;
            }

            var model = new InventoryManagementViewModel
            {
                Keyword = keyword,
                CategoryId = categoryId,
                StockStatus = stockStatus,
                TotalSku = totalSku,
                TotalUnitsInStock = totalUnits,
                LowStockCount = lowStockCount,
                OutOfStockCount = outOfStockCount,
                Categories = db.Categories.OrderBy(c => c.CategoryName).ToList(),
                Products = allProducts
                    .OrderBy(p => p.StockQuantity)
                    .ThenBy(p => p.ProductID)
                    .ToList()
            };

            return View(model);
        }

        // POST: /Inventory/ImportStock (Nhập thêm hàng mới vào kho)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ImportStock(int productId, int addQuantity)
        {
            if (addQuantity <= 0)
            {
                TempData["InventoryError"] = "Số lượng nhập thêm vào kho phải lớn hơn 0.";
                return RedirectToAction("Index");
            }

            var product = db.Products.Find(productId);
            if (product == null)
            {
                TempData["InventoryError"] = "Không tìm thấy sản phẩm trong kho.";
                return RedirectToAction("Index");
            }

            product.StockQuantity += addQuantity;
            db.SaveChanges();

            TempData["InventorySuccess"] = "Đã nhập thêm " + addQuantity + " sản phẩm cho \"" + product.ProductName + "\". Số lượng trong kho và đăng bán hiện tại: " + product.StockQuantity + ".";
            return RedirectToAction("Index");
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
