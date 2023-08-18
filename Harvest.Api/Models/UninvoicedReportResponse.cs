namespace Harvest.Api
{
    public class UninvoicedReportResponse : PagedList
    {
        public UninvoicedReport[] Results { get; set; }
    }
}
