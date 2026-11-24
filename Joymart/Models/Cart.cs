using System.Collections.Generic;
using System.Linq;

namespace Joymart.Models
{
    // Một dòng sản phẩm trong giỏ hàng Session
    public class CartItem
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public int StockQuantity { get; set; }

        public decimal LineTotal
        {
            get { return UnitPrice * Quantity; }
        }
    }

    // Toàn bộ giỏ hàng lưu trong Session
    public class Cart
    {
        private readonly List<CartItem> items = new List<CartItem>();

        public IEnumerable<CartItem> Items
        {
            get { return items; }
        }

        // Thêm sản phẩm vào giỏ hoặc tăng số lượng nếu đã tồn tại
        public bool Add(Product product)
        {
            if (product == null || !product.IsActive || product.StockQuantity <= 0)
            {
                return false;
            }

            var item = items.FirstOrDefault(x => x.ProductID == product.ProductID);
            if (item == null)
            {
                items.Add(new CartItem
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,
                    ImageUrl = product.ImageUrl,
                    UnitPrice = product.Price,
                    Quantity = 1,
                    StockQuantity = product.StockQuantity
                });
                return true;
            }

            if (item.Quantity < product.StockQuantity)
            {
                item.Quantity++;
                item.StockQuantity = product.StockQuantity;
                return true;
            }

            return false;
        }

        // Cập nhật số lượng một dòng sản phẩm
        public bool Update(int productId, int quantity, int currentStock)
        {
            var item = items.FirstOrDefault(x => x.ProductID == productId);
            if (item == null || quantity <= 0 || quantity > currentStock)
            {
                return false;
            }

            item.Quantity = quantity;
            item.StockQuantity = currentStock;
            return true;
        }

        // Xóa một sản phẩm khỏi giỏ
        public void Remove(int productId)
        {
            items.RemoveAll(x => x.ProductID == productId);
        }

        // Tổng số lượng sản phẩm trong giỏ
        public int TotalQuantity()
        {
            return items.Sum(x => x.Quantity);
        }

        // Tổng tiền toàn bộ giỏ hàng
        public decimal TotalMoney()
        {
            return items.Sum(x => x.LineTotal);
        }

        // Xóa toàn bộ giỏ hàng
        public void Clear()
        {
            items.Clear();
        }
    }
}