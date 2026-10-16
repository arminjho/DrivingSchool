using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace eAutoskola.Application.Reports;

/// <summary>Page frame and cell styles shared by both report PDFs: school name, generation time
/// and the applied filters on top of every page, page numbers at the bottom.</summary>
internal static class ReportPdfLayout
{
    public static readonly string Completed = Colors.Green.Darken1;
    public static readonly string Scheduled = Colors.Blue.Darken1;
    public static readonly string Cancelled = Colors.Red.Darken1;
    private static readonly string Muted = Colors.Grey.Darken1;

    static ReportPdfLayout()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] CreatePdf(
        string title,
        IReadOnlyList<(string Label, string Value)> appliedFilters,
        DateTime generatedAtUtc,
        Action<ColumnDescriptor> composeContent)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(header => ComposeHeader(header, title, appliedFilters, generatedAtUtc));
                page.Content().PaddingTop(10).Column(composeContent);
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, string title, IReadOnlyList<(string Label, string Value)> appliedFilters, DateTime generatedAtUtc)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(ReportTexts.SchoolName).FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                row.RelativeItem().AlignRight().AlignBottom()
                    .Text($"{ReportTexts.GeneratedLabel}: {generatedAtUtc:dd.MM.yyyy. HH:mm} ({ReportTexts.UtcSuffix})")
                    .FontSize(8).FontColor(Muted);
            });

            column.Item().PaddingTop(2).Text(title).FontSize(15).Bold();

            foreach (var (label, value) in appliedFilters)
            {
                column.Item().PaddingTop(2).Text(text =>
                {
                    text.Span($"{label}: ").SemiBold();
                    text.Span(value);
                });
            }

            column.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Medium);
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text(ReportTexts.SchoolName).FontSize(8).FontColor(Muted);
            row.RelativeItem().AlignRight().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(8).FontColor(Muted));
                text.Span($"{ReportTexts.PageLabel} ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    }

    public static void StatBox(IContainer container, string label, string value)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(column =>
        {
            column.Item().Text(label).FontSize(8).FontColor(Muted);
            column.Item().Text(value).FontSize(14).Bold();
        });
    }

    // Border first, padding inside it, so a border spans the whole cell with no gaps between columns.
    public static IContainer HeaderCell(IContainer container)
        => container.BorderBottom(1).BorderColor(Colors.Grey.Darken1)
            .DefaultTextStyle(x => x.Bold()).PaddingVertical(4).PaddingHorizontal(2);

    public static IContainer BodyCell(IContainer container)
        => container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(3).PaddingHorizontal(2);

    public static IContainer TotalCell(IContainer container)
        => container.BorderTop(1).BorderColor(Colors.Grey.Darken1)
            .DefaultTextStyle(x => x.Bold()).PaddingVertical(4).PaddingHorizontal(2);

    public static void LegendItem(RowDescriptor row, string color, string label)
    {
        row.AutoItem().PaddingRight(4).Width(9).Height(9).Background(color);
        row.AutoItem().PaddingRight(12).Text(label).FontSize(8);
    }
}
