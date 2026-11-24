namespace Joymart.Models
{
    public class ProductCardViewModel
    {
        public int ProductID { get; set; }          // Mã sản phẩm
        public string NamePro { get; set; }         // Tên sản phẩm
        public string CategoryName { get; set; }    // Tên danh mục
        public decimal Price { get; set; }          // Đơn giá
        public string ImagePro { get; set; }        // Tên file ảnh trong Content/images
        public string ShortDescription { get; set; } // Mô tả ngắn sản phẩm
        public int StockQuantity { get; set; }
    }
}