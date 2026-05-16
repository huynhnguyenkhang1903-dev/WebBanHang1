using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Websitebanhang.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }   // ✅ THÊM KHÓA CHÍNH

        public string? UserId { get; set; }

        public string CustomerName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";

        public string PaymentMethod { get; set; } = "COD";

        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }

        public string Status { get; set; } = OrderStatus.Pending;

        public string? ShippingProvider { get; set; }
        public decimal ShippingCost { get; set; }

        public bool IsPaid { get; set; } = false;
        public string? TransactionId { get; set; }
        public DateTime? PaidAt { get; set; }

        public string? PaymentContent { get; set; }

        public List<CartItem>? Items { get; set; }

        // Voucher fields
        public string? VoucherCode { get; set; }
        public int? VoucherDiscountPercent { get; set; }
        public DateTime? VoucherExpires { get; set; }

        // Shipping voucher fields
        public string? ShippingVoucherCode { get; set; }
        public int? ShippingVoucherDiscountPercent { get; set; }

        // Ghi chú đơn hàng
        public string? OrderNotes { get; set; }

        // Lý do hủy đơn
        public string? CancelReason { get; set; }

        // Trả hàng
        public string? ReturnReason { get; set; }
        public DateTime? ReturnRequestedAt { get; set; }
        public string? ReturnAdminNote { get; set; }
    }
}