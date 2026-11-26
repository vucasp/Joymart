using System.Collections.Generic;

namespace Joymart.Models
{
    // ViewModel hiển thị thẻ sản phẩm rút gọn trên lưới Trang chủ (Store/Index)
    public class ProductCardViewModel
    {
        public int ProductID { get; set; }
        public string NamePro { get; set; }
        public string CategoryName { get; set; }
        public decimal Price { get; set; }
        public string ImagePro { get; set; }
        public string ShortDescription { get; set; }
        public int StockQuantity { get; set; }
    }

    // ViewModel đầy đủ cho trang Chi tiết sản phẩm (Products/Details):
    //   - Thông tin sản phẩm chính
    //   - Danh sách tối đa 4 sản phẩm liên quan cùng danh mục
    //   - Danh sách đánh giá của người mua + Điểm trung bình + Quyền gửi đánh giá
    public class ProductDetailViewModel
    {
        public Product Product { get; set; }
        public List<Product> RelatedProducts { get; set; }
        public List<ProductReview> Reviews { get; set; }
        public double AverageRating { get; set; }
        public bool CanReview { get; set; }
        public ProductReview ExistingUserReview { get; set; }
    }
}
