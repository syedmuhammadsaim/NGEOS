using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using NGEOS.Desktop.Core.Entities;
using NGEOS.Desktop.Core.Interfaces;

namespace NGEOS.Desktop.Infrastructure.Export
{
    public class ClosedXmlExportService : IExcelExportService
    {
        public byte[] ExportDonationsToExcel(IReadOnlyList<Donation> donations)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Donations Ledger");

            // Header Banner
            ws.Cell(1, 1).Value = "NGEOS Humanitarian Platform — Official Donation Audit Ledger";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            ws.Range(1, 1, 1, 8).Merge().Style.Fill.BackgroundColor = XLColor.FromHtml("#0B192C");
            ws.Row(1).Height = 28;

            // Column Headers
            var headers = new[]
            {
                "Transaction ID", "Date (UTC)", "Donor Name", "Donor Email", "Cause Title", 
                "Payment Method", "Status", "Amount (PKR)"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(3, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FD5631");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            ws.Row(3).Height = 22;

            int row = 4;
            decimal totalAmount = 0;

            foreach (var d in donations)
            {
                ws.Cell(row, 1).Value = d.TransactionId;
                ws.Cell(row, 2).Value = d.DonationDate.ToString("yyyy-MM-dd HH:mm");
                ws.Cell(row, 3).Value = d.DonorName;
                ws.Cell(row, 4).Value = d.DonorEmail;
                ws.Cell(row, 5).Value = d.CauseTitle;
                ws.Cell(row, 6).Value = d.PaymentMethod;
                ws.Cell(row, 7).Value = d.PaymentStatus;

                var amountCell = ws.Cell(row, 8);
                amountCell.Value = d.Amount;
                amountCell.Style.NumberFormat.Format = "Rs. #,##0.00";
                amountCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // Subtle zebra striping
                if (row % 2 == 0)
                {
                    ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }

                if (d.PaymentStatus == "Completed")
                {
                    totalAmount += d.Amount;
                }
                row++;
            }

            // Totals Row
            var totalRow = ws.Row(row);
            totalRow.Height = 24;
            ws.Cell(row, 1).Value = "TOTAL COMPLETED DONATIONS";
            ws.Range(row, 1, row, 7).Merge().Style.Font.Bold = true;
            ws.Range(row, 1, row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");

            var sumCell = ws.Cell(row, 8);
            sumCell.Value = totalAmount;
            sumCell.Style.Font.Bold = true;
            sumCell.Style.NumberFormat.Format = "Rs. #,##0.00";
            sumCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        public byte[] ExportCausesToExcel(IReadOnlyList<Cause> causes)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Welfare Causes");

            ws.Cell(1, 1).Value = "NGEOS Humanitarian Platform — Welfare Causes Performance Report";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            ws.Range(1, 1, 1, 6).Merge().Style.Fill.BackgroundColor = XLColor.FromHtml("#0B192C");
            ws.Row(1).Height = 28;

            var headers = new[] { "ID", "Cause Title", "Category", "Target (PKR)", "Raised (PKR)", "Progress %" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(3, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FD5631");
                cell.Style.Font.FontColor = XLColor.White;
            }

            int row = 4;
            foreach (var c in causes)
            {
                ws.Cell(row, 1).Value = c.Id;
                ws.Cell(row, 2).Value = c.Title;
                ws.Cell(row, 3).Value = c.Category;
                ws.Cell(row, 4).Value = c.TargetAmount;
                ws.Cell(row, 4).Style.NumberFormat.Format = "Rs. #,##0.00";
                ws.Cell(row, 5).Value = c.RaisedAmount;
                ws.Cell(row, 5).Style.NumberFormat.Format = "Rs. #,##0.00";
                ws.Cell(row, 6).Value = $"{c.ProgressPercentage:F1}%";
                row++;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
