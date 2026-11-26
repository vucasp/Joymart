using System;
using System.Collections.Generic;
using System.Linq;

namespace Joymart.Models
{
    // =====================================================================
    // MÔ HÌNH GIỎ HÀNG TRONG SESSION (Cart & CartItem)
    // Bao gồm:
    //   - Quản lý thêm, sửa số lượng, xóa từng sản phẩm hoặc làm trống giỏ.
    //   - Đồng bộ giá bán & tồn kho mới nhất từ CSDL (SyncItem).
    //   - Tính tự động chính sách Miễn phí vận chuyển (đơn >= 360.067đ miễn phí,
    //     đơn dưới 360.067đ tính phí vận chuyển tiêu chuẩn 25.000đ).
    // =====================================================================

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

    public class Cart
    {
        // Hằng số mốc miễn phí vận chuyển (đồng bộ với thông báo trên thanh Topbar)
        public const decimal FreeShippingThreshold = 360067m;
        public const decimal StandardShippingFee = 25000m;

        private readonly List<CartItem> items = new List<CartItem>();

        public IEnumerable<CartItem> Items
        {
            get { return items; }
        }

        // Thêm 1 đơn vị sản phẩm vào giỏ (hoặc tăng số lượng nếu đã có trong giỏ)
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
                item.ProductName = product.ProductName;
                item.ImageUrl = product.ImageUrl;
                item.UnitPrice = product.Price;
                item.StockQuantity = product.StockQuantity;
                return true;
            }

            return false;
        }

        // Cập nhật số lượng của 1 mặt hàng trong giỏ, đảm bảo không vượt quá tồn kho
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

        // Đồng bộ lại thông tin, giá bán và tồn kho thực tế từ bảng Product trong CSDL
        public void SyncItem(Product product)
        {
            var item = items.FirstOrDefault(x => x.ProductID == product.ProductID);
            if (item == null) return;

            if (!product.IsActive || product.StockQuantity <= 0)
            {
                items.Remove(item);
                return;
            }

            item.ProductName = product.ProductName;
            item.ImageUrl = product.ImageUrl;
            item.UnitPrice = product.Price;
            item.StockQuantity = product.StockQuantity;
            if (item.Quantity > product.StockQuantity)
            {
                item.Quantity = product.StockQuantity;
            }
        }

        // Xóa 1 sản phẩm khỏi giỏ hàng
        public void Remove(int productId)
        {
            items.RemoveAll(x => x.ProductID == productId);
        }

        // Tổng số lượng sản phẩm trong giỏ
        public int TotalQuantity()
        {
            return items.Sum(x => x.Quantity);
        }

        // Tạm tính tổng tiền hàng (chưa gồm phí vận chuyển)
        public decimal TotalMoney()
        {
            return items.Sum(x => x.LineTotal);
        }

        // Tính phí vận chuyển: 0đ nếu đạt mốc Freeship 360.067đ, ngược lại 25.000đ
        public decimal ShippingFee()
        {
            if (!items.Any() || TotalMoney() >= FreeShippingThreshold)
            {
                return 0m;
            }
            return StandardShippingFee;
        }

        // Số tiền cần mua thêm để được hưởng Miễn phí vận chuyển
        public decimal AmountNeededForFreeShipping()
        {
            decimal remaining = FreeShippingThreshold - TotalMoney();
            return remaining > 0m ? remaining : 0m;
        }

        // Phần trăm tiến trình đạt mốc Freeship (0..100%)
        public int FreeShippingProgressPercent()
        {
            if (!items.Any()) return 0;
            decimal ratio = (TotalMoney() / FreeShippingThreshold) * 100m;
            return ratio >= 100m ? 100 : (int)Math.Round(ratio);
        }

        // Tổng số tiền khách cần thanh toán (Tiền hàng + Phí vận chuyển)
        public decimal GrandTotal()
        {
            return TotalMoney() + ShippingFee();
        }

        // Xóa toàn bộ giỏ hàng
        public void Clear()
        {
            items.Clear();
        }
    }
}
