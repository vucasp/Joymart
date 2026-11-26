namespace Joymart.Models
{
    using System;

    // Entity ánh xạ bảng ProductReview (Đánh giá & Bình luận của khách hàng đã mua sản phẩm)
    public partial class ProductReview
    {
        public int ReviewID { get; set; }
        public int ProductID { get; set; }
        public int CustomerID { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public System.DateTime CreatedAt { get; set; }

        public virtual Customer Customer { get; set; }
        public virtual Product Product { get; set; }
    }
}
