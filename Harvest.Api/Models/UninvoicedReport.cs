namespace Harvest.Api
{
    public class UninvoicedReport
    {
        public long ClientId { get; set; }
        public string ClientName { get; set; }
        public long ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Currency { get; set; }
        public decimal TotalHours { get; set; }
        public decimal UninvoicedHours { get; set; }
        public decimal UninvoicedExpenses { get; set; }
        public decimal UninvoicedAmount { get; set; }
        public bool IsFixedPriceProject { get; set; }
    }
}
