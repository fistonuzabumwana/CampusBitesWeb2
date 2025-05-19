// src/CampusBites.Web/Reporting/OrdersReportDocument.cs (or Infrastructure)
using CampusBites.Application.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers; // For Placeholders etc.
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;


namespace CampusBites.Web.Reporting;

public class OrdersReportDocument : IDocument
{
    private readonly List<OrderSummaryDto> _orders;
    private readonly Dictionary<string, string?> _userEmails;
    private readonly DateTimeOffset? _startDate;
    private readonly DateTimeOffset? _endDate;

    public OrdersReportDocument(List<OrderSummaryDto> orders, Dictionary<string, string?> userEmails, DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        _orders = orders;
        _userEmails = userEmails;
        _startDate = startDate;
        _endDate = endDate;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                // Setup page layout (margins, etc.)
                page.Margin(30); // Points (pt)
                page.PageColor(Colors.White);
                page.DefaultTextStyle(ts => ts.FontSize(10).FontFamily(Fonts.Calibri)); // Or other font

                // Replace the existing page.Header()... .Text(text => { ... }); block with this:
                page.Header()
                    .AlignCenter()
                    .PaddingBottom(10)
                    .Column(col => // Use a column for multi-line header
                    {
                        col.Item().Text("CampusBites - Orders Report").Bold().FontSize(16); // Title

                        // Build the date range string conditionally
                        string dateRangeString = string.Empty;
                        if (_startDate.HasValue || _endDate.HasValue)
                        {
                            dateRangeString = "Period: ";
                            if (_startDate.HasValue) dateRangeString += $"{_startDate.Value:yyyy-MM-dd}";
                            dateRangeString += " to ";
                            if (_endDate.HasValue) dateRangeString += $"{_endDate.Value:yyyy-MM-dd}";
                        }

                        // Display date range if applicable
                        if (!string.IsNullOrEmpty(dateRangeString))
                        {
                            col.Item().Text(dateRangeString); // Display the built string
                        }

                        // Display generation time
                        col.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}").Italic().FontSize(8);
                    });

                // Content
                page.Content()
                    .Column(column => // Using column for easy table placement
                    {
                        column.Spacing(15); // Space between elements

                        // Table definition
                        column.Item().Table(table =>
                        {
                            // Define Columns
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(40);  // ID
                                columns.RelativeColumn(2);   // Date
                                columns.RelativeColumn(3);   // User Email
                                columns.ConstantColumn(60);  // Total (Currency)
                                columns.RelativeColumn(1.5f); // Status
                                columns.ConstantColumn(40);  // # Items
                                columns.RelativeColumn(2);   // Payment Method
                                columns.RelativeColumn(2);   // Payment Ref
                            });

                            // Header Row
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("ID");
                                header.Cell().Element(CellStyle).Text("Date");
                                header.Cell().Element(CellStyle).Text("User Email");
                                header.Cell().Element(CellStyle).AlignRight().Text("Total");
                                header.Cell().Element(CellStyle).Text("Status");
                                header.Cell().Element(CellStyle).AlignCenter().Text("Items");
                                header.Cell().Element(CellStyle).Text("Pay Method");
                                header.Cell().Element(CellStyle).Text("Pay Ref");

                                static IContainer CellStyle(IContainer container) =>
                                    container.DefaultTextStyle(x => x.Bold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                            });

                            // Data Rows
                            foreach (var order in _orders)
                            {
                                _userEmails.TryGetValue(order.UserId, out var email); // Get email

                                table.Cell().Element(CellStyle).Text(order.Id.ToString());
                                table.Cell().Element(CellStyle).Text(order.OrderDate.ToString("yyyy-MM-dd HH:mm")); // Format date
                                table.Cell().Element(CellStyle).Text(email ?? "N/A");
                                table.Cell().Element(CellStyle).AlignRight().Text(order.OrderTotal.ToString("N2")); // Format currency (basic)
                                table.Cell().Element(CellStyle).Text(order.Status.ToString());
                                table.Cell().Element(CellStyle).AlignCenter().Text(order.NumberOfItems.ToString());
                                table.Cell().Element(CellStyle).Text(order.PaymentMethod ?? "-");
                                table.Cell().Element(CellStyle).Text(order.PaymentReference ?? "-");

                                static IContainer CellStyle(IContainer container) =>
                                     container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(5);
                            }
                        });

                        if (!_orders.Any())
                        {
                            column.Item().AlignCenter().Text("No orders found for the selected criteria.").Italic();
                        }
                    });

                // Footer
                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
            });
    }
}