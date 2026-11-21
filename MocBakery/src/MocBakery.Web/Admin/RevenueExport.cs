using ClosedXML.Excel;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using MocBakery.Models.DTOs.Reports;
using MocBakery.Web.Models;

namespace MocBakery.Web.Admin;

public static class RevenueExport
{
    private const string Note = "Số liệu từ hóa đơn thực tế, không cộng các yêu cầu mua chưa giao.";
    private static readonly Lazy<bool> Fonts = new(() =>
    {
        GlobalFontSettings.FontResolver = new ReportFonts();
        return true;
    });

    public static byte[] Excel(DateTime from, DateTime to, IReadOnlyList<RevenueDay> days, IReadOnlyList<ProductSales> products)
    {
        using var book = new XLWorkbook();
        IXLWorksheet Sheet(string name, string title)
        {
            var sheet = book.AddWorksheet(name);
            sheet.Style.Font.FontName = "Calibri";
            sheet.Style.Font.FontSize = 11;
            sheet.Range("A1:C1").Merge().Value = title;
            sheet.Cell("A1").Style.Font.SetBold().Font.FontSize = 18;
            sheet.Range("A2:C2").Merge().Value = $"Từ {from:dd/MM/yyyy} đến {to:dd/MM/yyyy}";
            sheet.Range("A3:C3").Merge().Value = "Mộc Bakery";
            sheet.Range("A4:C4").Merge().Value = Note;
            sheet.Range("A4:C4").Style.Alignment.WrapText = true;
            sheet.Row(1).Height = 30;
            sheet.Row(4).Height = 36;
            sheet.Column(1).Width = 48;
            sheet.Column(2).Width = 21;
            sheet.Column(3).Width = 26;
            sheet.SheetView.FreezeRows(7);
            sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            sheet.PageSetup.FitToPages(1, 0);
            return sheet;
        }
        void Header(IXLWorksheet sheet, params string[] labels)
        {
            for (int i = 0; i < labels.Length; i++) sheet.Cell(7, i + 1).Value = labels[i];
            var range = sheet.Range(7, 1, 7, labels.Length);
            range.Style.Fill.BackgroundColor = XLColor.FromHtml("#704C32");
            range.Style.Font.FontColor = XLColor.White;
            range.Style.Font.Bold = true;
            sheet.Row(7).Height = 25;
            sheet.PageSetup.SetRowsToRepeatAtTop(7, 7);
        }
        var summary = Sheet("Tổng hợp", "BÁO CÁO DOANH THU");
        summary.Cell("A6").Value = "Doanh thu trong kỳ";
        summary.Cell("B6").Value = days.Sum(x => x.Revenue);
        summary.Cell("B6").Style.NumberFormat.Format = "#,##0.00 \"đ\"";
        summary.Cell("A7").Value = "Số hóa đơn";
        summary.Cell("B7").Value = days.Sum(x => (long)x.InvoiceCount);
        summary.Range("A6:B7").Style.Font.Bold = true;

        var daily = Sheet("Theo ngày", "DOANH THU THEO NGÀY");
        Header(daily, "Ngày", "Số hóa đơn", "Doanh thu (VND)");
        for (int i = 0; i < days.Count; i++)
        {
            daily.Cell(i + 8, 1).Value = days[i].Day;
            daily.Cell(i + 8, 1).Style.DateFormat.Format = "dd/mm/yyyy";
            daily.Cell(i + 8, 2).Value = days[i].InvoiceCount;
            daily.Cell(i + 8, 3).Value = days[i].Revenue;
        }
        var best = Sheet("Bánh bán chạy", "BÁNH BÁN CHẠY");
        Header(best, "Bánh", "Số lượng đã bán", "Doanh thu (VND)");
        for (int i = 0; i < products.Count; i++)
        {
            // Assign text as a cell value, never as a formula, even for names starting with '='.
            best.Cell(i + 8, 1).Value = products[i].Name;
            best.Cell(i + 8, 2).Value = products[i].Quantity;
            best.Cell(i + 8, 3).Value = products[i].Revenue;
        }
        foreach (var (sheet, count) in new[] { (daily, days.Count), (best, products.Count) })
        {
            if (count == 0) sheet.Cell("A8").Value = "Không có hóa đơn trong kỳ.";
            else sheet.Range(7, 1, count + 7, 3).SetAutoFilter();
            sheet.Range(8, 1, Math.Max(8, count + 7), 3).Style.Alignment.WrapText = true;
            sheet.Range(8, 3, Math.Max(8, count + 7), 3).Style.NumberFormat.Format = "#,##0.00";
        }
        using var stream = new MemoryStream();
        book.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] Pdf(DateTime from, DateTime to, IReadOnlyList<RevenueDay> days, IReadOnlyList<ProductSales> products)
    {
        _ = Fonts.Value;
        var document = new Document();
        document.Info.Title = "Báo cáo doanh thu - Mộc Bakery";
        document.Info.Author = "Mộc Bakery";
        var normal = document.Styles[StyleNames.Normal]!;
        normal.Font.Name = "Noto Sans";
        normal.Font.Size = 10;
        normal.Font.Color = Color.FromRgb(48, 43, 38);
        normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(6);
        var heading = document.Styles[StyleNames.Heading1]!;
        heading.Font.Size = 13;
        heading.Font.Bold = true;
        heading.Font.Color = Color.FromRgb(112, 76, 50);
        heading.ParagraphFormat.SpaceBefore = Unit.FromPoint(18);
        heading.ParagraphFormat.SpaceAfter = Unit.FromPoint(10);
        var section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.TopMargin = Unit.FromCentimeter(1.8);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(1.8);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(1.8);
        section.PageSetup.RightMargin = Unit.FromCentimeter(1.8);
        var brand = section.AddParagraph("MỘC BAKERY");
        brand.Format.Font.Size = 11;
        brand.Format.Font.Color = Color.FromRgb(151, 117, 70);
        var title = section.AddParagraph("BÁO CÁO DOANH THU");
        title.Format.Font.Size = 20;
        title.Format.Font.Bold = true;
        section.AddParagraph($"Từ {from:dd/MM/yyyy} đến {to:dd/MM/yyyy}");
        var note = section.AddParagraph(Note);
        note.Format.Font.Size = 9;
        section.AddParagraph($"Doanh thu trong kỳ: {BakeryDisplay.Money(days.Sum(x => x.Revenue))}");
        section.AddParagraph($"Số hóa đơn: {days.Sum(x => (long)x.InvoiceCount)}");
        var footer = section.Footers.Primary.AddParagraph("Mộc Bakery  |  Trang ");
        footer.Format.Alignment = ParagraphAlignment.Right;
        footer.Format.Font.Size = 8;
        footer.AddPageField();
        footer.AddText(" / ");
        footer.AddNumPagesField();

        void Table(string label, string[] headers, IEnumerable<string[]> values)
        {
            section.AddParagraph(label, StyleNames.Heading1);
            var table = section.AddTable();
            table.Borders.Color = Color.FromRgb(226, 216, 202);
            table.Borders.Width = .5;
            table.TopPadding = Unit.FromPoint(7);
            table.BottomPadding = Unit.FromPoint(7);
            table.AddColumn(Unit.FromCentimeter(8.2));
            table.AddColumn(Unit.FromCentimeter(3.5));
            table.AddColumn(Unit.FromCentimeter(5.7));
            table.Columns[1].Format.Alignment = ParagraphAlignment.Right;
            table.Columns[2].Format.Alignment = ParagraphAlignment.Right;
            var head = table.AddRow();
            head.HeadingFormat = true;
            head.Shading.Color = Color.FromRgb(112, 76, 50);
            head.Format.Font.Color = Colors.White;
            head.Format.Font.Bold = true;
            for (int i = 0; i < 3; i++) head.Cells[i].AddParagraph(headers[i]);
            bool any = false;
            foreach (var cells in values)
            {
                any = true;
                var row = table.AddRow();
                for (int i = 0; i < 3; i++) row.Cells[i].AddParagraph(cells[i]);
            }
            if (!any)
            {
                var row = table.AddRow();
                row.Cells[0].MergeRight = 2;
                row.Cells[0].AddParagraph("Không có hóa đơn trong kỳ.");
            }
        }
        Table("Doanh thu theo ngày", ["Ngày", "Số hóa đơn", "Doanh thu"],
            days.Select(x => new[] { x.Day.ToString("dd/MM/yyyy"), x.InvoiceCount.ToString(), BakeryDisplay.Money(x.Revenue) }));
        Table("Bánh bán chạy", ["Bánh", "Số lượng", "Doanh thu"],
            products.Select(x => new[] { x.Name, x.Quantity.ToString(), BakeryDisplay.Money(x.Revenue) }));
        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        using var pdf = renderer.PdfDocument;
        pdf.Save(stream, false);
        return stream.ToArray();
    }

    private sealed class ReportFonts : IFontResolver
    {
        public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic) => new(bold ? "Bold" : "Regular");
        public byte[] GetFont(string faceName)
        {
            using var stream = typeof(RevenueExport).Assembly.GetManifestResourceStream($"MocBakery.Web.Assets.Fonts.NotoSans-{faceName}.ttf")
                ?? throw new InvalidOperationException("Missing embedded report font.");
            using var bytes = new MemoryStream();
            stream.CopyTo(bytes);
            return bytes.ToArray();
        }
    }
}
