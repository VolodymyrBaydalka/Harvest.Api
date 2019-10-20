using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public class InvoicePayment : BaseModel
    {
        public decimal Amount { get; set; } // The amount of the payment.
        public DateTime PaidAt { get; set; } // Date and time the payment was made.
        public DateTime PaidDate { get; set; } // Date the payment was made.
        public string RecordedBy { get; set; } // The name of the person who recorded the payment.
        public string RecordedByEmail { get; set; } // The email of the person who recorded the payment.
        public string Notes { get; set; } // Any notes associated with the payment.
        public string TransactionId { get; set; } // Either the card authorization or PayPal transaction ID.
        public IdNameModel PaymentGateway { get; set; } // The payment gateway id and name used to process the payment.
    }

    public class InvoicePaymentsReponse : PagedList
    {
        public InvoicePayment[] InvoicePayments { get; set; }
    }
}
