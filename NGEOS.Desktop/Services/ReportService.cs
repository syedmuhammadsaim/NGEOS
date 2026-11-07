using System.IO;
using ClosedXML.Excel;
using NGEOS.Desktop.Models;

namespace NGEOS.Desktop.Services
{
    public interface IReportService
    {
        string ExportDonationsToExcel(List<LocalDonation> donations, string filePath);
        string ExportCausesToExcel(List<LocalCause> causes, string filePath);
        string ExportProgrammesToExcel(List<LocalProgramme> programmes, string filePath);
        string GenerateHtmlReport(List<LocalDonation> donations, string title);
    }

    public class ReportService : IReportService
    {
        public string ExportDonationsToExcel(List<LocalDonation> donations, string filePath)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Donations Ledger");

            // Header styling
            worksheet.Cell(1, 1).Value = "Transaction ID";
            worksheet.Cell(1, 2).Value = "Donor Name";
            worksheet.Cell(1, 3).Value = "Donor Email";
            worksheet.Cell(1, 4).Value = "Cause / Campaign";
            worksheet.Cell(1, 5).Value = "Amount (PKR)";
            worksheet.Cell(1, 6).Value = "Payment Gateway";
            worksheet.Cell(1, 7).Value = "Status";
            worksheet.Cell(1, 8).Value = "Donation Date";

            var headerRange = worksheet.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(11, 25, 44);
            headerRange.Style.Font.FontColor = XLColor.White;

            int row = 2;
            foreach (var d in donations)
            {
                worksheet.Cell(row, 1).Value = d.TransactionId;
                worksheet.Cell(row, 2).Value = d.DonorName;
                worksheet.Cell(row, 3).Value = d.DonorEmail;
                worksheet.Cell(row, 4).Value = d.CauseTitle;
                worksheet.Cell(row, 5).Value = d.Amount;
                worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, 6).Value = d.PaymentGateway;
                worksheet.Cell(row, 7).Value = d.PaymentStatus;
                worksheet.Cell(row, 8).Value = d.DonationDate.ToString("yyyy-MM-dd HH:mm");
                row++;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
            return filePath;
        }

        public string ExportCausesToExcel(List<LocalCause> causes, string filePath)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Charity Causes");

            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "Title";
            worksheet.Cell(1, 3).Value = "Category";
            worksheet.Cell(1, 4).Value = "Target Amount (PKR)";
            worksheet.Cell(1, 5).Value = "Raised Amount (PKR)";
            worksheet.Cell(1, 6).Value = "Progress (%)";
            worksheet.Cell(1, 7).Value = "Status";

            var headerRange = worksheet.Range(1, 1, 1, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(11, 25, 44);
            headerRange.Style.Font.FontColor = XLColor.White;

            int row = 2;
            foreach (var c in causes)
            {
                worksheet.Cell(row, 1).Value = c.Id;
                worksheet.Cell(row, 2).Value = c.Title;
                worksheet.Cell(row, 3).Value = c.Category;
                worksheet.Cell(row, 4).Value = c.TargetAmount;
                worksheet.Cell(row, 5).Value = c.RaisedAmount;
                var pct = c.TargetAmount > 0 ? (c.RaisedAmount / c.TargetAmount) * 100 : 0;
                worksheet.Cell(row, 6).Value = $"{pct:F1}%";
                worksheet.Cell(row, 7).Value = c.IsActive ? "Active" : "Archived";
                row++;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
            return filePath;
        }

        public string ExportProgrammesToExcel(List<LocalProgramme> programmes, string filePath)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Programmes & Projects");

            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "Programme Title";
            worksheet.Cell(1, 3).Value = "Category";
            worksheet.Cell(1, 4).Value = "Location";
            worksheet.Cell(1, 5).Value = "Target Beneficiaries";
            worksheet.Cell(1, 6).Value = "Status";

            var headerRange = worksheet.Range(1, 1, 1, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(11, 25, 44);
            headerRange.Style.Font.FontColor = XLColor.White;

            int row = 2;
            foreach (var p in programmes)
            {
                worksheet.Cell(row, 1).Value = p.Id;
                worksheet.Cell(row, 2).Value = p.Title;
                worksheet.Cell(row, 3).Value = p.Category;
                worksheet.Cell(row, 4).Value = p.Location;
                worksheet.Cell(row, 5).Value = p.TargetBeneficiaries;
                worksheet.Cell(row, 6).Value = p.IsActive ? "Active" : "Inactive";
                row++;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
            return filePath;
        }

        public string GenerateHtmlReport(List<LocalDonation> donations, string title)
        {
            var total = donations.Sum(d => d.Amount);
            var completedCount = donations.Count(d => d.PaymentStatus == "Completed");

            var html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <title>{title} - NGEOS Audit Report</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 30px; color: #222; }}
        .header {{ display: flex; justify-content: space-between; border-bottom: 2px solid #fd5631; padding-bottom: 15px; margin-bottom: 25px; }}
        .logo {{ font-size: 26px; font-weight: bold; color: #0b192c; }}
        .badge {{ background: #fd5631; color: white; padding: 4px 10px; border-radius: 12px; font-size: 12px; }}
        .summary-cards {{ display: flex; gap: 20px; margin-bottom: 25px; }}
        .card {{ background: #f8fafc; border: 1px solid #e2e8f0; padding: 15px; border-radius: 8px; flex: 1; }}
        .card-num {{ font-size: 22px; font-weight: bold; color: #0b192c; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 10px; }}
        th {{ background: #0b192c; color: white; text-align: left; padding: 10px; font-size: 13px; }}
        td {{ padding: 10px; border-bottom: 1px solid #e2e8f0; font-size: 13px; }}
        tr:nth-child(even) {{ background: #f8fafc; }}
        .footer {{ margin-top: 40px; font-size: 11px; color: #64748b; text-align: center; border-top: 1px solid #e2e8f0; padding-top: 15px; }}
    </style>
</head>
<body>
    <div class='header'>
        <div>
            <div class='logo'>NGEOS</div>
            <small style='color: #64748b;'>Next-Generation NGO Management Platform</small>
        </div>
        <div style='text-align: right;'>
            <h3>{title}</h3>
            <span class='badge'>Official Audit Document</span>
            <div style='font-size: 12px; color: #64748b; margin-top: 4px;'>Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</div>
        </div>
    </div>

    <div class='summary-cards'>
        <div class='card'>
            <div>Total Volume</div>
            <div class='card-num'>Rs {total:N0}</div>
        </div>
        <div class='card'>
            <div>Total Records</div>
            <div class='card-num'>{donations.Count}</div>
        </div>
        <div class='card'>
            <div>Completed Contributions</div>
            <div class='card-num'>{completedCount}</div>
        </div>
    </div>

    <table>
        <thead>
            <tr>
                <th>Transaction ID</th>
                <th>Donor Name</th>
                <th>Cause / Campaign</th>
                <th>Gateway</th>
                <th>Amount</th>
                <th>Status</th>
                <th>Date</th>
            </tr>
        </thead>
        <tbody>";

            foreach (var d in donations)
            {
                html += $@"
            <tr>
                <td><strong>{d.TransactionId}</strong></td>
                <td>{d.DonorName}</td>
                <td>{d.CauseTitle}</td>
                <td>{d.PaymentGateway}</td>
                <td style='font-weight: bold; color: #16a34a;'>Rs {d.Amount:N2}</td>
                <td><span style='color: {(d.PaymentStatus == "Completed" ? "#16a34a" : "#ca8a04")}; font-weight: bold;'>{d.PaymentStatus}</span></td>
                <td>{d.DonationDate:yyyy-MM-dd HH:mm}</td>
            </tr>";
            }

            html += @"
        </tbody>
    </table>

    <div class='footer'>
        NGEOS Enterprise Certified Ledger &bull; All transactions qualify for charity audits & tax reporting &bull; Confirmed System Output
    </div>
</body>
</html>";

            var tempFile = Path.Combine(Path.GetTempPath(), $"NGEOS_Report_{Guid.NewGuid():N}.html");
            File.WriteAllText(tempFile, html);
            return tempFile;
        }
    }
}
