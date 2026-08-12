namespace RemoteITToolkit.Core.Entities
{
    public class ReportRecord : BaseEntity
    {
        public string FilePath { get; set; }
        public string ReportType { get; set; }
        public string OperatorName { get; set; }
    }
}