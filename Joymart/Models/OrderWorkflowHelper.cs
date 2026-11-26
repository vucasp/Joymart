using System;
using System.Linq;

namespace Joymart.Models
{
    // =====================================================================
    // LỚP TIỆN ÍCH NGHIỆP VỤ ĐƠN HÀNG (OrderWorkflowHelper)
    // Mục đích:
    //   Tự động kiểm tra và chuyển trạng thái các đơn hàng "Đang giao" sang
    //   "Hoàn thành" nếu đã quá 7 ngày kể từ ngày xuất hàng (ShippedDate) mà
    //   chưa được bấm xác nhận thủ công.
    // =====================================================================
    public static class OrderWorkflowHelper
    {
        public const int AutoCompleteAfterDays = 7;

        // Quét và tự động xác nhận "Hoàn thành" cho các đơn đang giao quá 7 ngày.
        // Trả về số lượng đơn hàng vừa được tự động hoàn thành.
        public static int SyncAutoCompletedOrders(DBJoymartEntities db)
        {
            if (db == null) return 0;

            DateTime cutoffTime = DateTime.Now.AddDays(-AutoCompleteAfterDays);

            var overdueOrders = db.OrderProes
                .Where(o => o.Status == "Đang giao" &&
                            ((o.ShippedDate.HasValue && o.ShippedDate.Value <= cutoffTime) ||
                             (!o.ShippedDate.HasValue && o.OrderDate <= cutoffTime)))
                .ToList();

            if (!overdueOrders.Any())
            {
                return 0;
            }

            foreach (var order in overdueOrders)
            {
                DateTime baseShipDate = order.ShippedDate ?? order.OrderDate;
                order.Status = "Hoàn thành";
                order.CompletedDate = baseShipDate.AddDays(AutoCompleteAfterDays);
            }

            db.SaveChanges();
            return overdueOrders.Count;
        }
    }
}
