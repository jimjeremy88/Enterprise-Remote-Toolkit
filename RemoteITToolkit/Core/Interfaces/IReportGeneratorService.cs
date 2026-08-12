using System.Threading.Tasks;

namespace RemoteITToolkit.Core.Interfaces
{
    public interface IReportGeneratorService
    {
        Task<string> GenerateEnterpriseReportAsync(string technicianName, string companyName, bool incApps, bool incServices, bool incEvents);
    }
}