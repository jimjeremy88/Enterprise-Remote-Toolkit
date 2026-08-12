using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RemoteITToolkit.Core.Interfaces;

namespace RemoteITToolkit.Services
{
    public class ReportGeneratorService : IReportGeneratorService
    {
        private readonly ISettingsService _settings;
        private readonly IExtendedLogger _logger;
        private readonly ISystemAnalyzerService _sysAnalyzer;
        private readonly INetworkToolsService _netTools;
        private readonly ISystemQueryService _queryService;

        public ReportGeneratorService(ISettingsService settings, IExtendedLogger logger, ISystemAnalyzerService sysAnalyzer, INetworkToolsService netTools, ISystemQueryService queryService)
        {
            _settings = settings; _logger = logger; _sysAnalyzer = sysAnalyzer; _netTools = netTools; _queryService = queryService;
        }

        public async Task<string> GenerateEnterpriseReportAsync(string technicianName, string companyName, bool incApps, bool incServices, bool incEvents)
        {
            string folder = _settings.ExportFolder;
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, $"IT_Audit_{Environment.MachineName}_{DateTime.Now:yyyyMMdd_HHmm}.pdf");

            var hwTask = _sysAnalyzer.GetHardwareInfoAsync();
            var netTask = _netTools.GetNetworkInfoAsync();
            var appsTask = incApps ? _queryService.GetInstalledProgramsAsync() : Task.FromResult(Enumerable.Empty<RemoteITToolkit.Core.DTOs.InstalledProgramDTO>());
            var svcTask = incServices ? _queryService.GetWindowsServicesAsync() : Task.FromResult(Enumerable.Empty<RemoteITToolkit.Core.DTOs.WindowsServiceDTO>());
            var evtTask = incEvents ? _queryService.GetRecentEventLogsAsync("System", 100) : Task.FromResult(Enumerable.Empty<RemoteITToolkit.Core.DTOs.SystemEventLogDTO>());

            await Task.WhenAll(hwTask, netTask, appsTask, svcTask, evtTask);

            var hw = hwTask.Result; var net = netTask.Result; var apps = appsTask.Result.ToList(); var svcs = svcTask.Result.Where(s => s.Status == "Running").ToList(); var evts = evtTask.Result.Where(e => e.EntryType == "Error" || e.EntryType == "Critical").ToList();

            await Task.Run(() =>
            {
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 40, 40, 80, 60))
                using (var writer = iTextSharp.text.pdf.PdfWriter.GetInstance(doc, fs))
                {
                    writer.PageEvent = new PdfHeaderFooter(companyName, technicianName);
                    doc.Open();

                    var titleFont = iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA_BOLD, 18, iTextSharp.text.BaseColor.DARK_GRAY);
                    var headerFont = iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA_BOLD, 14, iTextSharp.text.BaseColor.BLACK);
                    var normalFont = iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA, 10, iTextSharp.text.BaseColor.BLACK);
                    var tableHeaderFont = iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA_BOLD, 10, iTextSharp.text.BaseColor.WHITE);

                    var pTitle = new iTextSharp.text.Paragraph("SYSTEM AUDIT & DIAGNOSTIC REPORT", titleFont);
                    pTitle.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
                    pTitle.SpacingAfter = 20;
                    doc.Add(pTitle);

                    var p1 = new iTextSharp.text.Paragraph("1. System Overview", headerFont); p1.SpacingAfter = 10; doc.Add(p1);
                    var sysTable = CreateTable(2); AddRow(sysTable, "Hostname", net.Hostname, normalFont); AddRow(sysTable, "Operating System", $"{hw.WindowsVersion} (Build {hw.WindowsBuild})", normalFont); AddRow(sysTable, "Uptime", $"{hw.Uptime.Days} Days", normalFont); AddRow(sysTable, "Antivirus", hw.Antivirus, normalFont); doc.Add(sysTable);

                    var p2 = new iTextSharp.text.Paragraph("2. Hardware Diagnostics", headerFont); p2.SpacingBefore = 15; p2.SpacingAfter = 10; doc.Add(p2);
                    var hwTable = CreateTable(2); AddRow(hwTable, "Processor (CPU)", hw.CpuName, normalFont); AddRow(hwTable, "Memory (RAM)", $"{hw.AvailableRam} Avail / {hw.InstalledRam} Total", normalFont); AddRow(hwTable, "Graphics (GPU)", hw.GpuName, normalFont); AddRow(hwTable, "Motherboard", hw.Motherboard, normalFont); AddRow(hwTable, "Serial Number", hw.SerialNumber, normalFont);
                    var sysDrive = hw.LogicalDrives.FirstOrDefault(d => d.DriveLetter.StartsWith("C")); if (sysDrive != null) AddRow(hwTable, "System Drive (C:)", $"{sysDrive.UsagePercentage} Used", normalFont); doc.Add(hwTable);

                    var p3 = new iTextSharp.text.Paragraph("3. Network Configuration", headerFont); p3.SpacingBefore = 15; p3.SpacingAfter = 10; doc.Add(p3);
                    var netTable = CreateTable(2); AddRow(netTable, "Active Adapter", net.ActiveAdapter, normalFont); AddRow(netTable, "MAC Address", net.MacAddress, normalFont); AddRow(netTable, "Local IP", net.LocalIp, normalFont); AddRow(netTable, "Public IP", net.PublicIp, normalFont); doc.Add(netTable);

                    if (incEvents && evts.Count > 0)
                    {
                        var p4 = new iTextSharp.text.Paragraph("4. Critical Event Logs", headerFont); p4.SpacingBefore = 15; p4.SpacingAfter = 10; doc.Add(p4);
                        var evtTable = new iTextSharp.text.pdf.PdfPTable(3) { WidthPercentage = 100 }; evtTable.SetWidths(new float[] { 25f, 25f, 50f });
                        evtTable.AddCell(GetHeaderCell("Date", tableHeaderFont)); evtTable.AddCell(GetHeaderCell("Source", tableHeaderFont)); evtTable.AddCell(GetHeaderCell("Message", tableHeaderFont));
                        foreach (var e in evts) { evtTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(e.TimeGenerated.ToString("g"), normalFont)) { Padding = 5 }); evtTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(e.Source, normalFont)) { Padding = 5 }); evtTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(e.Message.Replace("\n", " "), normalFont)) { Padding = 5 }); }
                        doc.Add(evtTable);
                    }

                    if (incApps && apps.Count > 0)
                    {
                        doc.NewPage();
                        var p5 = new iTextSharp.text.Paragraph("5. Installed Software", headerFont); p5.SpacingAfter = 10; doc.Add(p5);
                        var appTable = new iTextSharp.text.pdf.PdfPTable(3) { WidthPercentage = 100 }; appTable.SetWidths(new float[] { 40f, 40f, 20f });
                        appTable.AddCell(GetHeaderCell("Application Name", tableHeaderFont)); appTable.AddCell(GetHeaderCell("Publisher", tableHeaderFont)); appTable.AddCell(GetHeaderCell("Version", tableHeaderFont));
                        foreach (var app in apps) { appTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(app.Name, normalFont)) { Padding = 4 }); appTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(app.Publisher, normalFont)) { Padding = 4 }); appTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(app.Version, normalFont)) { Padding = 4 }); }
                        doc.Add(appTable);
                    }
                    doc.Close();
                }
            });

            _logger.LogAudit(technicianName, $"Generated PDF Report: {path}");
            return path;
        }

        private iTextSharp.text.pdf.PdfPTable CreateTable(int columns) { var table = new iTextSharp.text.pdf.PdfPTable(columns) { WidthPercentage = 100 }; table.SetWidths(new float[] { 30f, 70f }); return table; }
        private iTextSharp.text.pdf.PdfPCell GetHeaderCell(string text, iTextSharp.text.Font font) => new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(text, font)) { BackgroundColor = new iTextSharp.text.BaseColor(2, 132, 199), Padding = 6, HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER };
        private void AddRow(iTextSharp.text.pdf.PdfPTable table, string col1, string col2, iTextSharp.text.Font font) { table.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(col1, iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA_BOLD, 10, iTextSharp.text.BaseColor.BLACK))) { Padding = 5, BackgroundColor = new iTextSharp.text.BaseColor(241, 245, 249) }); table.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(col2, font)) { Padding = 5 }); }
    }

    public class PdfHeaderFooter : iTextSharp.text.pdf.PdfPageEventHelper
    {
        private readonly string _companyName; private readonly string _techName;
        public PdfHeaderFooter(string companyName, string techName) { _companyName = string.IsNullOrWhiteSpace(companyName) ? "Enterprise IT Solutions" : companyName; _techName = string.IsNullOrWhiteSpace(techName) ? "System Administrator" : techName; }

        public override void OnEndPage(iTextSharp.text.pdf.PdfWriter writer, iTextSharp.text.Document document)
        {
            var cb = writer.DirectContent; var font = iTextSharp.text.pdf.BaseFont.CreateFont(iTextSharp.text.pdf.BaseFont.HELVETICA, iTextSharp.text.pdf.BaseFont.CP1252, iTextSharp.text.pdf.BaseFont.NOT_EMBEDDED);
            cb.BeginText(); cb.SetFontAndSize(font, 14); cb.SetColorFill(new iTextSharp.text.BaseColor(2, 132, 199)); cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"[ {_companyName.ToUpper()} ]", document.Left, document.Top + 15, 0); cb.SetFontAndSize(font, 9); cb.SetColorFill(iTextSharp.text.BaseColor.GRAY); cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_RIGHT, "Confidential System Audit", document.Right, document.Top + 15, 0); cb.EndText();
            cb.BeginText(); cb.SetFontAndSize(font, 9); cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm} | Technician: {_techName}", document.Left, document.Bottom - 15, 0); cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_RIGHT, $"Page {writer.PageNumber}", document.Right, document.Bottom - 15, 0); cb.EndText();
            cb.SetLineWidth(1f); cb.SetColorStroke(iTextSharp.text.BaseColor.LIGHT_GRAY); cb.MoveTo(document.Left, document.Top + 10); cb.LineTo(document.Right, document.Top + 10); cb.Stroke(); cb.MoveTo(document.Left, document.Bottom - 5); cb.LineTo(document.Right, document.Bottom - 5); cb.Stroke();
        }
    }
}