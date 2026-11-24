using Joymart.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Joymart.Controllers
{
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
            // Quy định mỗi trang hiển thị tối đa 6 sản phẩm
            const int pageSize = 6;
            if (page < 1)
            {
                page = 1;
            }

            // 1. Bắt đầu từ các sản phẩm đang kinh doanh
            var products = db.Products.Where(p => p.IsActive);

            // 2. Lọc theo danh mục (nếu có)
            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryID == categoryId.Value);
            }

            // 3. Tìm kiếm theo tên hoặc mô tả sản phẩm (nếu có)
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                products = products.Where(p => p.ProductName.Contains(keyword)
                                            || p.Description.Contains(keyword));
            }

            // 4. Lọc theo khoảng giá từ - đến (nếu có)
            if (minPrice.HasValue)
            {
                products = products.Where(p => p.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.Price <= maxPrice.Value);
            }

            // 5. Đếm tổng số sản phẩm thỏa điều kiện & tính số trang
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

            // 6. Sắp xếp trước khi Skip/Take (Bài tập 2)
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

            // 7. Đóng gói dữ liệu vào StoreHomeViewModel
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

        // GET: /Store/AboutStore (Giữ nguyên từ Buổi 04)
        public ActionResult AboutStore()
        {
            return View();
        }
    }
}