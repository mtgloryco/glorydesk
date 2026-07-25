using System.Collections.Generic;

namespace InventoryManagementSystem.Services
{
    /// <summary>
    /// Plain-English vs classic accounting label presets for TerminologyOverrides.
    /// </summary>
    public static class PlainLanguagePresets
    {
        public static IReadOnlyDictionary<string, string> SimpleEnglish { get; } = new Dictionary<string, string>
        {
            ["Product"] = "Product",
            ["Customer"] = "Customer",
            ["Supplier"] = "Supplier",
            ["Category"] = "Category",
            ["Order"] = "Order",
            ["Invoice"] = "Customer Bill",
            ["SalesOrder"] = "Customer Order",
            ["SalesQuotation"] = "Price Quote",
            ["PurchaseOrder"] = "Buy Order",
            ["Rfq"] = "Ask Supplier for Price",
            ["CreditNote"] = "Customer Refund Credit",
            ["DebitNote"] = "Supplier Refund",
            ["DeliveryNote"] = "Delivery Slip",
            ["PackingSlip"] = "Packing List",
            ["AccountsReceivable"] = "Money Customers Owe You",
            ["AccountsPayable"] = "Money You Owe Suppliers",
            ["Vat"] = "Sales Tax",
            ["Cogs"] = "Cost of Stock Sold",
            ["Journal"] = "Money Record",
            ["ChartOfAccounts"] = "Money Categories",
            ["BankReconciliation"] = "Match Bank to Payments",
            ["RecurringInvoice"] = "Repeat Bill",
            ["ConfirmShipment"] = "Mark as Sent",
            ["BillingStatus"] = "Payment Status",
            ["PurchaseOrders"] = "Orders to Suppliers",
            ["Forecasting"] = "Demand Forecast",
            ["ReorderDashboard"] = "What to Reorder",
            ["ExpiryDashboard"] = "Expiring Stock",
            ["Returns"] = "Returns & Refunds",
            ["AuditTrail"] = "Activity Log",
            ["Manufacturing"] = "Making Products",
            ["POS"] = "Checkout / Cashier",
            ["Inventory"] = "Products & Stock",
            ["Rep_Profit"] = "Income vs Expenses"
        };

        public static IReadOnlyDictionary<string, string> AccountingTerms { get; } = new Dictionary<string, string>
        {
            ["Product"] = "Product",
            ["Customer"] = "Customer",
            ["Supplier"] = "Supplier",
            ["Category"] = "Category",
            ["Order"] = "Order",
            ["Invoice"] = "Invoice",
            ["SalesOrder"] = "Sales Order",
            ["SalesQuotation"] = "Sales Quotation",
            ["PurchaseOrder"] = "Purchase Order",
            ["Rfq"] = "Request for Quotation (RFQ)",
            ["CreditNote"] = "Credit Note",
            ["DebitNote"] = "Debit Note",
            ["DeliveryNote"] = "Delivery Note",
            ["PackingSlip"] = "Packing Slip",
            ["AccountsReceivable"] = "Accounts Receivable (AR)",
            ["AccountsPayable"] = "Accounts Payable (AP)",
            ["Vat"] = "VAT",
            ["Cogs"] = "COGS",
            ["Journal"] = "Journal Entry",
            ["ChartOfAccounts"] = "Chart of Accounts",
            ["BankReconciliation"] = "Bank Reconciliation",
            ["RecurringInvoice"] = "Recurring Invoice",
            ["ConfirmShipment"] = "Confirm Shipment",
            ["BillingStatus"] = "Billing Status",
            ["PurchaseOrders"] = "Purchase Orders",
            ["Forecasting"] = "Forecasting",
            ["ReorderDashboard"] = "Reorder Dashboard",
            ["ExpiryDashboard"] = "Expiry Dashboard",
            ["Returns"] = "Returns",
            ["AuditTrail"] = "Audit Trail",
            ["Manufacturing"] = "Manufacturing",
            ["POS"] = "POS / Cashier",
            ["Inventory"] = "Inventory",
            ["Rep_Profit"] = "Profit & Loss"
        };
    }
}
