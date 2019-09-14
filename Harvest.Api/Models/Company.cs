using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Harvest.Api
{
    public class Company
    {
        public string BaseUri { get; set; }
        public string FullDomain { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public WeekStartDayType WeekStartDay { get; set; }
        public bool WantsTimestampTimers { get; set; }
        public TimeFormatType TimeFormat { get; set; }
        public string PlanType { get; set; }
        public ClockType Clock { get; set; }
        public bool InvoiceFeature { get; set; }
        public bool EstimateFeature { get; set; }
        public bool ApprovalFeature { get; set; }
        public string DecimalSymbol { get; set; }
        public string ThousandsSeparator { get; set; }
        public string ColorScheme { get; set; }
    }

    public enum ClockType
    {
        [EnumMember(Value = "12h")]
        TwelveHour,
        [EnumMember(Value = "24h")]
        TwentyFourHour
    }

    public enum TimeFormatType
    {
        Decimal,
        HoursMinutes
    }

    public enum WeekStartDayType
    {
        Saturday,
        Sunday,
        Monday
    }
}
