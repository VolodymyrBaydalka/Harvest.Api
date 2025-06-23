using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public class Invoice : BaseModel
    {
        public Client Client { get; set; } // An object containing invoice’s client id and name.
        public LineItem[] LineItems { get; set; } // Array of invoice line items.
        public IdNameModel Estimate { get; set; } // An object containing the associated estimate’s id.
        public IdNameModel Retainer { get; set; } // An object containing the associated retainer’s id.
        public NullableIdNameModel Creator { get; set; } // An object containing the id and name of the person that created the invoice.
        public string ClientKey { get; set; } // Used to build a URL to the public web invoice for your client:https://{ACCOUNT_SUBDOMAIN}.harvestapp.com/client/invoices/abc123456
        public string Number { get; set; } // If no value is set, the number will be automatically generated.
        public string PurchaseOrder { get; set; } // The purchase order number.
        public decimal Amount { get; set; } // The total amount for the invoice, including any discounts and taxes.
        public decimal DueAmount { get; set; } // The total amount due at this time for this invoice.
        public decimal? Tax { get; set; } // This percentage is applied to the subtotal, including line items and discounts.
        public decimal? TaxAmount { get; set; } // The first amount of tax included, calculated from tax. If no tax is defined, this value will be null.
        public decimal? Tax2 { get; set; } // This percentage is applied to the subtotal, including line items and discounts.
        public decimal? Tax2Amount { get; set; } // The amount calculated from tax2.
        public decimal? Discount { get; set; } // This percentage is subtracted from the subtotal.
        public decimal? DiscountAmount { get; set; } // The amount calcuated from discount.
        public string Subject { get; set; } // The invoice subject.
        public string Notes { get; set; } // Any additional notes included on the invoice.
        public string Currency { get; set; } // The currency code associated with this invoice.
        public InvoiceState State { get; set; } // The current state of the invoice: draft, open, paid, or closed.
        public DateTime? PeriodStart { get; set; } // Start of the period during which time entries were added to this invoice.
        public DateTime? PeriodEnd { get; set; } // End of the period during which time entries were added to this invoice.
        public DateTime? IssueDate { get; set; } // Date the invoice was issued.
        public DateTime? DueDate { get; set; } // Date the invoice is due.
        public string PaymentTerm { get; set; } // The timeframe in which the invoice should be paid. Options: upon receipt, net 15, net 30, net 45, net 60, or custom.
        public DateTime? SentAt { get; set; } // Date and time the invoice was sent.
        public DateTime? PaidAt { get; set; } // Date and time the invoice was paid.
        public DateTime? PaidDate { get; set; } // Date the invoice was paid.
        public DateTime? ClosedAt { get; set; } // Date and time the invoice was closed.
    }

    public class LineItem
    {
        public int? Id { get; set; } // Unique ID for the line item.
        public ProjectReference Project { get; set; } // An object containing the associated project’s id, name, and code.
        public string Kind { get; set; } // The name of an invoice item category.
        public string Description { get; set; } // Text description of the line item.
        public decimal Quantity { get; set; } // The unit quantity of the item.
        public decimal UnitPrice { get; set; } // The individual price per unit.
        public decimal Amount { get; set; } // The line item subtotal (quantity * unit_price).
        public bool Taxed { get; set; } // Whether the invoice’s tax percentage applies to this line item.
        public bool Taxed2 { get; set; } // Whether the invoice’s tax2 percentage applies to this line item.

        // Undocumented fields
        [Undocumented]
        public bool? _Destory { get; set; } // Whether the line item will be deleted
    }

    public class InvoicesResponse : PagedList
    {
        public Invoice[] Invoices { get; set; }
    }


    public enum InvoiceState
    {
        Draft,
        Open,
        Paid,
        Closed
    }

    public class LineItemParam
    {
        public long? ProjectId { get; set; } // The ID of the project associated with this line item.
        public string Kind { get; set; } // The name of an invoice item category.
        public string Description { get; set; } // Text description of the line item.
        public decimal Quantity { get; set; } // The unit quantity of the item.
        public decimal UnitPrice { get; set; } // The individual price per unit.
        public bool Taxed { get; set; } // Whether the invoice’s tax percentage applies to this line item.
        public bool Taxed2 { get; set; } // Whether the invoice’s tax2 percentage applies to this line item.
        // Undocumented fields
        [Undocumented]
        public bool? _Destory { get; set; } // Whether the line item will be deleted
    }
}
