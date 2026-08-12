namespace RemoteITToolkit.Core.Entities
{
    public class ApplicationLog : BaseEntity
    {
        public string LogLevel { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }
}