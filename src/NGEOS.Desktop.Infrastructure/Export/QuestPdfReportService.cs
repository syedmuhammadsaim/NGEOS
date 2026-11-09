using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NGEOS.Desktop.Core.Entities;
using NGEOS.Desktop.Core.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NGEOS.Desktop.Infrastructure.Export
{
    public class QuestPdfReportService : IReportService
    {
        public QuestPdfReportService()
        {
            // Set QuestPDF Community License for open-source / community development
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.UseSystemFonts = true;
            QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
        }

        public byte[] GenerateDonationReceiptPdf(Donation donation)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(titleCol =>
                            {
                                titleCol.Item().Text("NGEOS FOUNDATION").Bold().FontSize(22).FontColor("#FD5631");
                                titleCol.Item().Text("Next-Generation NGO & Welfare Alliance").FontSize(10).FontColor(Colors.Grey.Medium);
                                titleCol.Item().Text("Official Tax-Exempt Charitable Receipt").Bold().FontSize(12).FontColor("#0B192C");
                            });

                            row.ConstantItem(150).Column(rcpCol =>
                            {
                                rcpCol.Item().Border(1).BorderColor("#FD5631").Padding(6).Column(box =>
                                {
                                    box.Item().Text("RECEIPT #").Bold().FontSize(8).FontColor("#FD5631");
                                    box.Item().Text(donation.ReceiptNumber).Bold().FontSize(11).FontColor("#0B192C");
                                    box.Item().Text($"Date: {donation.DonationDate:dd MMM yyyy}").FontSize(8).FontColor(Colors.Grey.Darken1);
                                });
                            });
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    // Body
                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Text("DONOR INFORMATION").Bold().FontSize(11).FontColor("#0B192C");
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Donor Name: {donation.DonorName}").Bold();
                                c.Item().Text($"Donor Email: {donation.DonorEmail}");
                                if (!string.IsNullOrEmpty(donation.DonorPhone))
                                    c.Item().Text($"Phone: {donation.DonorPhone}");
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Transaction ID: {donation.TransactionId}").Bold();
                                c.Item().Text($"Payment Method: {donation.PaymentMethod}");
                                c.Item().Text($"Status: {donation.PaymentStatus}").FontColor(Colors.Green.Darken2).Bold();
                            });
                        });

                        col.Item().PaddingTop(16).Text("CONTRIBUTION BREAKDOWN").Bold().FontSize(11).FontColor("#0B192C");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(40);
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(2);
                                cols.ConstantColumn(120);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#0B192C").Padding(6).Text("#").Bold().FontColor(Colors.White);
                                header.Cell().Background("#0B192C").Padding(6).Text("Cause / Initiative").Bold().FontColor(Colors.White);
                                header.Cell().Background("#0B192C").Padding(6).Text("Allocation Purpose").Bold().FontColor(Colors.White);
                                header.Cell().Background("#0B192C").Padding(6).AlignRight().Text("Amount (PKR)").Bold().FontColor(Colors.White);
                            });

                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Text("1");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Text(donation.CauseTitle).Bold();
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Text(donation.Notes ?? "Humanitarian Welfare & Direct Aid");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(8).AlignRight().Text($"Rs. {donation.Amount:N2}").Bold();
                        });

                        // Total Box
                        col.Item().PaddingTop(10).AlignRight().Border(1).BorderColor(Colors.Grey.Lighten1).Background("#F8FAFC").Padding(10).Row(row =>
                        {
                            row.RelativeItem().Text("TOTAL CONTRIBUTION:").Bold().FontSize(13).FontColor("#0B192C");
                            row.ConstantItem(120).AlignRight().Text($"Rs. {donation.Amount:N2}").Bold().FontSize(14).FontColor("#FD5631");
                        });

                        // Formal Tax Exemption Notice
                        col.Item().PaddingTop(24).Border(1).BorderColor("#CBD5E1").Background("#F1F5F9").Padding(10).Column(c =>
                        {
                            c.Item().Text("TAX EXEMPTION & COMPLIANCE ACKNOWLEDGMENT").Bold().FontSize(9).FontColor("#0B192C");
                            c.Item().Text("This donation is made to NGEOS Foundation, a registered charitable non-profit alliance. No goods or services were provided in exchange for this contribution. Contributions are tax-exempt under applicable humanitarian welfare provisions.").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });

                        // Signatures
                        col.Item().PaddingTop(30).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);
                                c.Item().PaddingTop(4).Text("Executive Director / Secretariat Signatory").FontSize(8).FontColor(Colors.Grey.Darken1);
                            });

                            row.ConstantItem(40);

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);
                                c.Item().PaddingTop(4).Text("Chief Financial & Audit Officer").FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                        });
                    });

                    // Footer
                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("NGEOS Foundation • Generated automatically via NGEOS Desktop System • Page ");
                        t.CurrentPageNumber();
                        t.Span(" of ");
                        t.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateAuditReportPdf(IReadOnlyList<Donation> donations, IReadOnlyList<Cause> causes, decimal totalRaised, int totalDonors)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("NGEOS FOUNDATION").Bold().FontSize(20).FontColor("#FD5631");
                                c.Item().Text("Executive Financial & Operational Audit Report").Bold().FontSize(12).FontColor("#0B192C");
                            });

                            row.ConstantItem(150).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Generated: {DateTime.Now:dd MMM yyyy, HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
                                c.Item().Text("Status: Certified Official Audit").Bold().FontSize(8).FontColor(Colors.Green.Darken2);
                            });
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        // KPI Summary Boxes
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(8).Column(c =>
                            {
                                c.Item().Text("TOTAL FUNDS RAISED").FontSize(8).FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"Rs. {totalRaised:N0}").Bold().FontSize(14).FontColor("#FD5631");
                            });

                            row.ConstantItem(10);

                            row.RelativeItem().Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(8).Column(c =>
                            {
                                c.Item().Text("RECORDED DONORS").FontSize(8).FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"{totalDonors:N0} Contributors").Bold().FontSize(14).FontColor("#0B192C");
                            });

                            row.ConstantItem(10);

                            row.RelativeItem().Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(8).Column(c =>
                            {
                                c.Item().Text("ACTIVE CAUSES").FontSize(8).FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"{causes.Count(x => x.IsActive)} Campaigns").Bold().FontSize(14).FontColor(Colors.Green.Darken2);
                            });
                        });

                        // Donations Table
                        col.Item().PaddingTop(16).Text("RECENT AUDIT DONATIONS LEDGER").Bold().FontSize(10).FontColor("#0B192C");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(85);
                                cols.ConstantColumn(75);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.ConstantColumn(80);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Background("#0B192C").Padding(4).Text("TXN ID").Bold().FontColor(Colors.White);
                                h.Cell().Background("#0B192C").Padding(4).Text("Date").Bold().FontColor(Colors.White);
                                h.Cell().Background("#0B192C").Padding(4).Text("Donor").Bold().FontColor(Colors.White);
                                h.Cell().Background("#0B192C").Padding(4).Text("Cause").Bold().FontColor(Colors.White);
                                h.Cell().Background("#0B192C").Padding(4).AlignRight().Text("Amount").Bold().FontColor(Colors.White);
                            });

                            foreach (var d in donations.Take(25))
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(d.TransactionId).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(d.DonationDate.ToString("yyyy-MM-dd")).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(d.DonorName).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(d.CauseTitle).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"Rs. {d.Amount:N0}").Bold().FontSize(8);
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("NGEOS Foundation Confidential Report • Page ");
                        t.CurrentPageNumber();
                        t.Span(" of ");
                        t.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
