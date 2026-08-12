using System.Collections.Generic;
using RemoteITToolkit.Core.DTOs;

namespace RemoteITToolkit.Core.Interfaces
{
    public interface IExtendedLogger : ILogger
    {
        void LogSecurity(string message, string operatorName = "System");
        void LogUserAction(string action, string operatorName = "System");
        void LogNetwork(string message);

        void LogActivity(string activityMessage);
        void LogAudit(string operatorName, string action);

        IEnumerable<LogEntryDTO> GetRecentLogs(int limit = 1000);
    }
}