using Joymart.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Joymart.Controllers
{
    // =====================================================================
    // CONTROLLER TRANG CHỦ MUA SẮM & GIỚI THIỆU (StoreController)
    // Nhiệm vụ:
    //   - Hiển thị danh sách sản phẩm đang kinh doanh (mỗi trang 9 sản phẩm).
    //   - Hỗ trợ lọc theo danh mục, từ khóa tên/mô tả, khoảng giá và sắp xếp.
    // =====================================================================
    public class StoreController : Controller
    {
        private DBJoymartEntities db = new DBJoymartEntities();

        // GET: /Store/Index
        public ActionResult Index(
            int? categoryId,
            string keyword,
            decimal? minPrice,
            decimal? maxPrice,
            string sortOrder,
            int page = 1)
        {
            // Quy định mỗi trang hiển thị tối đa 9 sản phẩm (lưới 3 cột x 3 hàng)
            const int pageSize = 9;
            if (page < 1)
            {
                page = 1;
            }

            var products = db.Products.Where(p => p.IsActive);

            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryID == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                products = products.Where(p => p.ProductName.Contains(keyword)
                                            || p.Description.Contains(keyword));
            }

            if (minPrice.HasValue)
            {
                products = products.Where(p => p.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.Price <= maxPrice.Value);
            }

            int totalItems = products.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            if (totalPages == 0)
            {
                totalPages = 1;
            }
            if (page > totalPages)
            {
                page = totalPages;
            }

            switch (sortOrder)
            {
                case "price_asc":
                    products = products.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    products = products.OrderByDescending(p => p.Price);
                    break;
                default:
                    products = products.OrderByDescending(p => p.ProductID);
                    break;
            }

            var model = new StoreHomeViewModel
            {
                Categories = db.Categories
                    .OrderBy(c => c.CategoryName)
                    .ToList(),

                Products = products
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProductCardViewModel
                    {
                        ProductID = p.ProductID,
                        NamePro = p.ProductName,
                        CategoryName = p.Category.CategoryName,
                        Price = p.Price,
                        ImagePro = p.ImageUrl,
                        ShortDescription = p.Description,
                        StockQuantity = p.StockQuantity
                    })
                    .ToList(),

                SelectedCategoryId = categoryId,
                SearchTerm = keyword,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortOrder = sortOrder,
                TotalItems = totalItems,
                CurrentPage = page,
                TotalPages = totalPages
            };

            return View(model);
        }

        // GET: /Store/AboutStore
        public ActionResult AboutStore()
        {
            return View();
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
