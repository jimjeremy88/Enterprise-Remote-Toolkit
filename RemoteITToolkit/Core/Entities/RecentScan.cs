using System;

namespace RemoteITToolkit.Core.Entities
{
    public class RecentScan : BaseEntity
    {
        public DateTime ScanDate { get; set; }
        public string DeviceName { get; set; }
        public string Summary { get; set; }
    }
}