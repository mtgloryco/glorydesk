using System;

namespace InventoryManagementSystem.Domain
{
    /// <summary>
    /// One denormalised row per POS order line, used as the flat source for the
    /// Point of Sale → Reporting pivot / graph (see <c>PosReportPivot</c>).
    /// Order-level measures (order count, order total) are de-duplicated by
    /// <see cref="OrderId"/> inside each pivot cell.
    /// </summary>
    public class PosOrderReportRow
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }

        public string Cashier { get; set; } = "None";
        public string PaymentMethod { get; set; } = "None";
        public string SessionNumber { get; set; } = "None";
        public string CustomerName { get; set; } = "Walk-in Customer";

        public string ProductName { get; set; } = "None";
        public string ProductCategory { get; set; } = "None";
        public string Currency { get; set; } = string.Empty;

        /// <summary>Units sold on this line.</summary>
        public int Quantity { get; set; }

        /// <summary>Line value (unit price × quantity).</summary>
        public decimal LineTotal { get; set; }

        /// <summary>Whole-order total, repeated on every line of the order.</summary>
        public decimal OrderTotal { get; set; }
    }
}
