using QuestPDF.Fluent;

namespace eAutoskola.Application.Reports;

internal static class LessonStatusReportPdf
{
    private const float ChartBarHeight = 12;
    private const float ChartLabelWidth = 120;
    private const float ChartTotalWidth = 28;

    public static byte[] Render(LessonStatusReportData data)
    {
        var filters = new List<(string, string)>
        {
            (ReportTexts.PeriodLabel, ReportTexts.FormatPeriod(data.DateFrom, data.DateTo)),
            (ReportTexts.InstructorLabel, data.InstructorName ?? ReportTexts.AllInstructors),
        };

        return ReportPdfLayout.CreatePdf(ReportTexts.LessonReportTitle, filters, data.GeneratedAtUtc, column =>
        {
            if (data.TotalLessons == 0)
            {
                column.Item().Text(ReportTexts.NoLessons);
                return;
            }

            column.Item().Row(row =>
            {
                row.Spacing(8);
                row.RelativeItem().Element(c => ReportPdfLayout.StatBox(c, ReportTexts.TotalLabel, data.TotalLessons.ToString()));
                row.RelativeItem().Element(c => ReportPdfLayout.StatBox(c, ReportTexts.CompletedColumn, data.TotalCompleted.ToString()));
                row.RelativeItem().Element(c => ReportPdfLayout.StatBox(c, ReportTexts.ScheduledColumn, data.TotalScheduled.ToString()));
                row.RelativeItem().Element(c => ReportPdfLayout.StatBox(c, ReportTexts.CancelledColumn, data.TotalCancelled.ToString()));
            });

            column.Item().PaddingTop(14).Text(ReportTexts.ChartTitle).FontSize(11).Bold();
            column.Item().PaddingTop(4).Row(legend =>
            {
                ReportPdfLayout.LegendItem(legend, ReportPdfLayout.Completed, ReportTexts.CompletedColumn);
                ReportPdfLayout.LegendItem(legend, ReportPdfLayout.Scheduled, ReportTexts.ScheduledColumn);
                ReportPdfLayout.LegendItem(legend, ReportPdfLayout.Cancelled, ReportTexts.CancelledColumn);
            });
            ComposeChart(column, data);

            column.Item().PaddingTop(14).Text(ReportTexts.TableTitle).FontSize(11).Bold();
            column.Item().PaddingTop(4).Element(c => ComposeTable(c, data));

            column.Item().PaddingTop(8).Text(ReportTexts.ScheduledFootnote).FontSize(8).Italic();
        });
    }

    /// <summary>One stacked horizontal bar per instructor, all drawn on the same scale (the
    /// busiest instructor's total fills the bar area), so bar lengths are comparable.</summary>
    private static void ComposeChart(ColumnDescriptor column, LessonStatusReportData data)
    {
        var busiest = data.Rows.Max(r => r.Total);

        foreach (var row in data.Rows)
        {
            column.Item().PaddingTop(4).Row(line =>
            {
                line.ConstantItem(ChartLabelWidth).AlignMiddle().Text(row.InstructorName).FontSize(8);
                line.RelativeItem().AlignMiddle().Row(bar =>
                {
                    if (row.Completed > 0)
                    {
                        bar.RelativeItem(row.Completed).Height(ChartBarHeight).Background(ReportPdfLayout.Completed);
                    }
                    if (row.Scheduled > 0)
                    {
                        bar.RelativeItem(row.Scheduled).Height(ChartBarHeight).Background(ReportPdfLayout.Scheduled);
                    }
                    if (row.Cancelled > 0)
                    {
                        bar.RelativeItem(row.Cancelled).Height(ChartBarHeight).Background(ReportPdfLayout.Cancelled);
                    }
                    if (busiest > row.Total)
                    {
                        bar.RelativeItem(busiest - row.Total).Height(ChartBarHeight);
                    }
                });
                line.ConstantItem(ChartTotalWidth).AlignMiddle().AlignRight().Text(row.Total.ToString()).FontSize(8).Bold();
            });
        }
    }

    private static void ComposeTable(QuestPDF.Infrastructure.IContainer container, LessonStatusReportData data)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(4);
                columns.RelativeColumn(1.4f);
                columns.RelativeColumn(1.4f);
                columns.RelativeColumn(1.4f);
                columns.RelativeColumn(1.2f);
            });

            table.Header(header =>
            {
                header.Cell().Element(ReportPdfLayout.HeaderCell).Text(ReportTexts.InstructorLabel);
                header.Cell().Element(ReportPdfLayout.HeaderCell).AlignRight().Text(ReportTexts.CompletedColumn);
                header.Cell().Element(ReportPdfLayout.HeaderCell).AlignRight().Text(ReportTexts.ScheduledColumn);
                header.Cell().Element(ReportPdfLayout.HeaderCell).AlignRight().Text(ReportTexts.CancelledColumn);
                header.Cell().Element(ReportPdfLayout.HeaderCell).AlignRight().Text(ReportTexts.TotalLabel);
            });

            foreach (var row in data.Rows)
            {
                table.Cell().Element(ReportPdfLayout.BodyCell).Text(row.InstructorName);
                table.Cell().Element(ReportPdfLayout.BodyCell).AlignRight().Text(row.Completed.ToString());
                table.Cell().Element(ReportPdfLayout.BodyCell).AlignRight().Text(row.Scheduled.ToString());
                table.Cell().Element(ReportPdfLayout.BodyCell).AlignRight().Text(row.Cancelled.ToString());
                table.Cell().Element(ReportPdfLayout.BodyCell).AlignRight().Text(row.Total.ToString());
            }

            table.Cell().Element(ReportPdfLayout.TotalCell).Text(ReportTexts.TotalLabel);
            table.Cell().Element(ReportPdfLayout.TotalCell).AlignRight().Text(data.TotalCompleted.ToString());
            table.Cell().Element(ReportPdfLayout.TotalCell).AlignRight().Text(data.TotalScheduled.ToString());
            table.Cell().Element(ReportPdfLayout.TotalCell).AlignRight().Text(data.TotalCancelled.ToString());
            table.Cell().Element(ReportPdfLayout.TotalCell).AlignRight().Text(data.TotalLessons.ToString());
        });
    }
}
