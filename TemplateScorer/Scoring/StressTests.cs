using IronPdf;
using SkiaSharp;
using TemplateScorer.Invoices;

namespace TemplateScorer.Scoring;

public enum StressTest
{
    BackgroundsOff,
    LongRow,
    ManyLines,
    EuroLongNames,
}

/// <summary>
/// The stress round. Every template gets the same harder input, and the checks look at the
/// PDF. Nothing in a model's template is changed.
/// </summary>
public sealed partial class TemplateScorer
{
    public async Task<ScoreCard> StressAsync(StressTest test, string model, string templatePath, string outputFolder)
    {
        var template = await File.ReadAllTextAsync(templatePath);
        var run = Path.GetFileNameWithoutExtension(templatePath);

        return test switch
        {
            StressTest.BackgroundsOff => await BackgroundsOffAsync(model, run, template, outputFolder),
            StressTest.LongRow => await LongRowAsync(model, run, template, outputFolder),
            StressTest.ManyLines => await RenderAndCheckAsync(model, run, template, outputFolder,
                StressInvoices.WithManyLines(InvoiceStore.GetSample())),
            StressTest.EuroLongNames => await RenderAndCheckAsync(model, run, template, outputFolder,
                StressInvoices.InEurosWithLongNames(InvoiceStore.GetSample())),
            _ => throw new ArgumentOutOfRangeException(nameof(test)),
        };
    }

    /// <summary>
    /// Renders the same invoice with background printing on and off and compares the pixels.
    /// PrintHtmlBackgrounds = false is the renderer default and matches a browser printing with
    /// "Background graphics" unticked. A template that sets print-color-adjust: exact should
    /// come out the same both ways.
    /// </summary>
    private async Task<ScoreCard> BackgroundsOffAsync(string model, string run, string template, string outputFolder)
    {
        var html = InvoiceFiller.Fill(template, InvoiceStore.GetSample());

        // Same markup with every background forced off. If that looks identical to the normal
        // render, the template prints no fills at all and there is nothing for this test to protect.
        var stripped = html.Replace("</head>", "<style>* { background: none !important; }</style></head>");

        var on = await _renderer.RenderHtmlAsPdfAsync(html);
        var off = await _noBackgrounds.RenderHtmlAsPdfAsync(html);
        var none = await _renderer.RenderHtmlAsPdfAsync(stripped);

        Directory.CreateDirectory(outputFolder);
        var onPixels = Rasterize(on, Path.Combine(outputFolder, $"{run}-bg-on"));
        var offPixels = Rasterize(off, Path.Combine(outputFolder, $"{run}-bg-off"));
        var nonePixels = Rasterize(none, Path.Combine(outputFolder, $"{run}-bg-none"));

        var fills = PercentChanged(onPixels, nonePixels);
        var lost = PercentChanged(onPixels, offPixels);
        onPixels.Concat(offPixels).Concat(nonePixels).ToList().ForEach(b => b.Dispose());

        var check = (fills, lost) switch
        {
            ( < 0.01, _) => new Check("Backgrounds kept", null, "template prints no background fills"),
            (_, < 0.01) => new Check("Backgrounds kept", true, $"fills on {fills:0.00}% of the page, all kept"),
            _ => new Check("Backgrounds kept", false, $"fills on {fills:0.00}% of the page, {lost:0.00}% of pixels lost them"),
        };

        return new ScoreCard(model, run, on.PageCount, [check]);
    }

    /// <summary>
    /// Share of pixels that differ between two renders. The threshold is low on purpose:
    /// invoice fills are often very light greys like #f4f4f4, only 11 steps from white.
    /// </summary>
    private static double PercentChanged(List<SKBitmap> first, List<SKBitmap> second)
    {
        long changed = 0, total = 0;
        for (var p = 0; p < Math.Min(first.Count, second.Count); p++)
        {
            var a = first[p];
            var b = second[p];
            for (var y = 0; y < Math.Min(a.Height, b.Height); y++)
            for (var x = 0; x < Math.Min(a.Width, b.Width); x++)
            {
                total++;
                if (Distance(a.GetPixel(x, y), b.GetPixel(x, y)) > 3) changed++;
            }
        }

        return total == 0 ? 0 : 100.0 * changed / total;
    }

    /// <summary>
    /// Puts a multi-line row right where page one ends. The position depends on the template,
    /// so it is found by rendering the normal invoice first and taking the last row on page one.
    /// </summary>
    private async Task<ScoreCard> LongRowAsync(string model, string run, string template, string outputFolder)
    {
        var sample = InvoiceStore.GetSample();
        var probe = await _renderer.RenderHtmlAsPdfAsync(InvoiceFiller.Fill(template, sample));
        var firstPage = Normalize(probe.ExtractTextFromPage(0));

        var lastOnPageOne = sample.Lines.ToList().FindLastIndex(l => firstPage.Contains(Normalize(l.Sku)));
        if (lastOnPageOne < 0) lastOnPageOne = 0;

        var invoice = StressInvoices.WithLongRow(sample, lastOnPageOne);
        var card = await RenderAndCheckAsync(model, run, template, outputFolder, invoice);

        var pdf = PdfDocument.FromFile(Path.Combine(outputFolder, $"{run}.pdf"));
        var pages = Enumerable.Range(0, pdf.PageCount).Select(i => Normalize(pdf.ExtractTextFromPage(i))).ToArray();

        return card with { Checks = [.. card.Checks, CheckLongRowIntact(pages)] };
    }

    /// <summary>
    /// The first and last words of the long description are unique in the invoice. If they
    /// land on different pages, the row was sliced by the page break.
    /// </summary>
    private static Check CheckLongRowIntact(string[] pages)
    {
        var start = PageOf(pages, "autumn launch bundle");
        var end = PageOf(pages, "printed barista starter guide");

        if (start < 0 || end < 0) return new Check("Long row intact", false, "long row text not found in the PDF");

        return new Check("Long row intact", start == end,
            start == end ? $"whole on page {start + 1}" : $"starts on page {start + 1}, ends on page {end + 1}");
    }

    private async Task<ScoreCard> RenderAndCheckAsync(string model, string run, string template, string outputFolder, Invoice invoice)
    {
        PdfDocument pdf;
        try
        {
            pdf = await _renderer.RenderHtmlAsPdfAsync(InvoiceFiller.Fill(template, invoice));
        }
        catch (Exception ex)
        {
            return new ScoreCard(model, run, 0, [new Check("Renders", false, ex.Message)]);
        }

        var pages = Enumerable.Range(0, pdf.PageCount)
            .Select(i => Normalize(pdf.ExtractTextFromPage(i)))
            .ToArray();

        Directory.CreateDirectory(outputFolder);
        await File.WriteAllBytesAsync(Path.Combine(outputFolder, $"{run}.pdf"), pdf.BinaryData);
        Rasterize(pdf, Path.Combine(outputFolder, run), maxPages: 3).ForEach(b => b.Dispose());

        return new ScoreCard(model, run, pdf.PageCount,
        [
            CheckHeaderRepeats(template, pages, invoice),
            CheckTotalsTogether(pages, invoice),
            CheckRowsIntact(pages, invoice),
        ]);
    }

    /// <summary>Writes {prefix}-page-N.png per page and hands the bitmaps back.</summary>
    private static List<SKBitmap> Rasterize(PdfDocument pdf, string prefix, int maxPages = int.MaxValue)
    {
        var bitmaps = new List<SKBitmap>();
        var count = Math.Min(pdf.PageCount, maxPages);

        pdf.RasterizeToImageFiles($"{prefix}-tmp-*.png", Enumerable.Range(0, count), DPI: 72);
        for (var i = 1; i <= count; i++)
        {
            var target = $"{prefix}-page-{i}.png";
            File.Move($"{prefix}-tmp-{i}.png", target, overwrite: true);
            bitmaps.Add(SKBitmap.Decode(target));
        }

        return bitmaps;
    }

    private static int Distance(SKColor a, SKColor b) =>
        Math.Max(Math.Abs(a.Red - b.Red), Math.Max(Math.Abs(a.Green - b.Green), Math.Abs(a.Blue - b.Blue)));
}
