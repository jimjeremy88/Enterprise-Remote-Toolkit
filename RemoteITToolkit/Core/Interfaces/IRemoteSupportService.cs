using System.Collections.Generic;
using System.Threading.Tasks;

namespace RemoteITToolkit.Core.Interfaces
{
    public interface IRemoteSupportService
    {
        void LaunchRDP();
        void LaunchQuickAssist();
        void LaunchRemoteAssistance();

        void LaunchAnyDesk();
        void LaunchTeamViewer();
        void LaunchRustDesk();

        string GetSupportSummaryText();

        Task<string> CaptureScreenshotAsync(string exportFolder);
        void StartClipboardMonitor();
        IEnumerable<string> GetClipboardHistory();
    }
}