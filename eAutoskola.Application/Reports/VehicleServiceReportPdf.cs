using eAutoskola.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace eAutoskola.Application.Reports;

internal static class VehicleServiceReportPdf
{
    public static byte[] Render(VehicleServiceReportData data)
    {
        var filters = new List<(string, string)>
        {
            (ReportTexts.VehicleCategoryLabel, data.VehicleCategoryName ?? ReportTexts.AllCategories),
            (ReportTexts.PeriodLabel, ReportTexts.FormatPeriod(data.DateFrom, data.DateTo)),
        };

        return ReportPdfLayout.CreatePdf(ReportTexts.VehicleReportTitle, filters, data.GeneratedAtUtc, column =>
        {
            if (data.TotalVehicles == 0)
            {
                column.Item().Text(ReportTexts.NoVehicles);
                return;
            }

            column.Item().Row(row =>
            {
                row.Spacing(8);
                row.RelativeItem().Element(c => ReportPdfLayout.StatBox(c, ReportTexts.VehicleCountLabel, data.TotalVehicles.ToString()));
                row.RelativeItem().Element(c => ReportPdfLayout.StatBox(c, ReportTexts.ServiceCountLabel, data.TotalServices.ToString()));
                row.RelativeItem().Element(c => ReportPdfLayout.StatBox(c, ReportTexts.TotalCostLabel, ReportTexts.FormatMoney(data.TotalCost)));
            });

            column.Item().PaddingTop(6).Text(text =>
            {
                text.Span($"{ReportTexts.ServicesByStatusTitle}: ").SemiBold();
                text.Span(string.Join(", ", Enum.GetValues<VehicleServiceStatus>()
                    .Select(status => $"{ReportTexts.VehicleServiceStatusLabel(status)} {data.CountServices(status)}")));
            });

            column.Item().PaddingTop(14).Text(ReportTexts.CostByCategoryTitle).FontSize(11).Bold();
            column.Item().PaddingTop(4).Element(c => ComposeCategoryTable(c, data));

            column.Item().PaddingTop(14).Text(ReportTexts.ServiceHistoryTitle).FontSize(11).Bold();
            foreach (var vehicle in data.Vehicles)
            {
                column.Item().PaddingTop(10).EnsureSpace(70).Column(block => ComposeVehicle(block, vehicle));
            }
        });
    }

    private static void ComposeCategoryTable(IContainer container, VehicleServiceReportData data)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(4);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
                columns.RelativeColumn(2);
            });

            table.Header(header =>
            {
                header.Cell().Element(ReportPdfLayout.HeaderCell).Text(ReportTexts.CategoryColumn);
                header.Cell().Element(ReportPdfLayout.HeaderCell).AlignRight().Text(ReportTexts.VehiclesColumn);
                header.Cell().Element(ReportPdfLayout.HeaderCell).AlignRight().Text(ReportTexts.ServicesColumn);
                header.Cell().Element(ReportPdfLayout.HeaderCell).AlignRight().Text(ReportTexts.CostColumn);
            });

            foreach (var category in data.Categories)
            {
                table.Cell().Element(ReportPdfLayout.BodyCell).Text($"{category.CategoryCode} - {category.CategoryName}");
                table.Cell().Element(ReportPdfLayout.BodyCell).AlignRight().Text(category.VehicleCount.ToString());
                table.Cell().Element(ReportPdfLayout.BodyCell).AlignRight().Text(category.ServiceCount.ToString());
                table.Cell().Element(ReportPdfLayout.BodyCell).AlignRight().Text(ReportTexts.FormatMoney(category.TotalCost));
            }

            table.Cell().Element(ReportPdfLayout.TotalCell).Text(ReportTexts.TotalLabel);
            table.Cell().Element(ReportPdfLayout.TotalCell).AlignRight().Text(data.TotalVehicles.ToString());
            table.Cell().Element(ReportPdfLayout.TotalCell).AlignRight().Text(data.TotalServices.ToString());
            table.Cell().Element(ReportPdfLayout.TotalCell).AlignRight().Text(ReportTexts.FormatMoney(data.TotalCost));
        });
    }

    private static void ComposeVehicle(ColumnDescriptor block, VehicleServiceReportVehicle vehicle)
    {
        block.Item().Background(Colors.Grey.Lighten3).Padding(5).Text(text =>
        {
            text.Span(vehicle.RegistrationNumber).Bold();
            text.Span($"  |  {vehicle.Make} {vehicle.Model}  |  {vehicle.CategoryCode} - {vehicle.CategoryName}  |  {vehicle.ManufactureYear}  |  {ReportTexts.VehicleStatusLabel(vehicle.Status)}");
        });

        if (vehicle.Services.Count == 0)
        {
            block.Item().PaddingTop(4).PaddingLeft(5).Text(ReportTexts.NoServices).Italic();
            return;
        }

        block.Item().PaddingTop(2).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1.5f);
                columns.RelativeColumn(2.5f);
                columns.RelativeColumn(4);
                columns.RelativeColumn(1.6f);
                columns.RelativeColumn(1.8f);
            });

            table.Header(header =>
            {
                header.Cell().Element(ReportPdfLayout.HeaderCell).Text(ReportTexts.DateColumn);
                header.Cell().Element(ReportPdfLayout.HeaderCell).Text(ReportTexts.ServiceTypeColumn);
                header.Cell().Element(ReportPdfLayout.HeaderCell).Text(ReportTexts.DescriptionColumn);
                header.Cell().Element(ReportPdfLayout.HeaderCell).Text(ReportTexts.StatusColumn);
                header.Cell().Element(ReportPdfLayout.HeaderCell).AlignRight().Text(ReportTexts.CostColumn);
            });

            foreach (var service in vehicle.Services)
            {
                table.Cell().Element(ReportPdfLayout.BodyCell).Text(ReportTexts.FormatDate(service.ServiceDate));
                table.Cell().Element(ReportPdfLayout.BodyCell).Text(service.ServiceTypeName);
                table.Cell().Element(ReportPdfLayout.BodyCell).Text(service.Description);
                table.Cell().Element(ReportPdfLayout.BodyCell).Text(ReportTexts.VehicleServiceStatusLabel(service.Status));
                table.Cell().Element(ReportPdfLayout.BodyCell).AlignRight().Text(ReportTexts.FormatMoney(service.Cost));
            }

            table.Cell().ColumnSpan(4).Element(ReportPdfLayout.TotalCell).Text(ReportTexts.VehicleSubtotal);
            table.Cell().Element(ReportPdfLayout.TotalCell).AlignRight().Text(ReportTexts.FormatMoney(vehicle.TotalCost));
        });
    }
}
