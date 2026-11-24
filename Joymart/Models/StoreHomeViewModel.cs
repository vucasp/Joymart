using System.Collections.Generic;

namespace Joymart.Models
{
    public class StoreHomeViewModel
    {
        // Danh sách danh mục lấy động từ bảng Category
        public List<Category> Categories { get; set; }

        // Danh sách sản phẩm hiển thị trên trang hiện tại
        public List<ProductCardViewModel> Products { get; set; }

        // Các điều kiện lọc & tìm kiếm đang áp dụng
        public int? SelectedCategoryId { get; set; }
        public string SearchTerm { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        // Sắp xếp theo giá (Bài tập 2)
        public string SortOrder { get; set; }

        // Tổng số sản phẩm tìm thấy (Bài tập 1)
        public int TotalItems { get; set; }

        // Thông tin phân trang
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}