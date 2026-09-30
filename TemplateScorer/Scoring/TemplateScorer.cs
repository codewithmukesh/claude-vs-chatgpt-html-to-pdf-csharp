using System.Net;
using System.Text.RegularExpressions;
using IronPdf;
using IronPdf.Rendering;
using TemplateScorer.Invoices;

namespace TemplateScorer.Scoring;

/// <summary>
/// A single rubric check. Passed is null when the check does not apply,
/// for example "header repeats on page 2" on a one page document.
/// </summary>
public sealed record Check(string Name, bool? Passed, string Detail);

public sealed record ScoreCard(string Model, string Run, int PageCount, IReadOnlyList<Check> Checks)
{
    public int Score => Checks.Count(c => c.Passed == true);

    public int OutOf => Checks.Count(c => c.Passed is not null);
}

/// <summary>
/// Renders a model-written template with the 46 line sample invoice and checks the PDF,
/// not the markup. Most of what goes wrong with a print template only shows up once the
/// document is long enough to run onto a second page.
/// </summary>
public sealed partial class TemplateScorer
{
    private readonly ChromePdfRenderer _renderer = CreateRenderer();
    private readonly ChromePdfRenderer _noBackgrounds = CreateRenderer(printBackgrounds: false);

    public async Task<ScoreCard> ScoreAsync(string model, string templatePath, string outputFolder)
    {
        var template = await File.ReadAllTextAsync(templatePath);
        var css = StripComments(string.Join('\n', StyleBlock().Matches(template).Select(m => m.Groups[1].Value)));
        var run = Path.GetFileNameWithoutExtension(templatePath);
        var invoice = InvoiceStore.GetSample();

        var checks = new List<Check>
        {
            CheckSelfContained(template),
            CheckPageRule(css),
            CheckPlaceholders(template),
            CheckHeaderOverride(css),
            CheckTabularNumbers(css),
        };

        PdfDocument pdf;
        try
        {
            pdf = await _renderer.RenderHtmlAsPdfAsync(InvoiceFiller.Fill(template, invoice));
        }
        catch (Exception ex)
        {
            checks.Add(new Check("Renders", false, ex.Message));
            return new ScoreCard(model, run, 0, checks);
        }

        checks.Add(new Check("Renders", true, "OK"));

        var pages = Enumerable.Range(0, pdf.PageCount)
            .Select(i => Normalize(pdf.ExtractTextFromPage(i)))
            .ToArray();

        checks.Add(CheckHeaderRepeats(template, pages, invoice));
        checks.Add(CheckTotalsTogether(pages, invoice));
        checks.Add(CheckRowsIntact(pages, invoice));

        Directory.CreateDirectory(outputFolder);
        await File.WriteAllBytesAsync(Path.Combine(outputFolder, $"{run}.pdf"), pdf.BinaryData);
        if (pdf.PageCount > 1)
        {
            // Page 2 is where print templates break, so that is the page worth looking at.
            // IronPDF numbers the output files from 1, not by page index, so rename it.
            var page2 = Path.Combine(outputFolder, $"{run}-page-2.png");
            pdf.RasterizeToImageFiles(Path.Combine(outputFolder, $"{run}-tmp-*.png"), [1], DPI: 110);
            File.Move(Path.Combine(outputFolder, $"{run}-tmp-1.png"), page2, overwrite: true);
        }

        return new ScoreCard(model, run, pdf.PageCount, checks);
    }

    private static Check CheckSelfContained(string html)
    {
        var problems = new List<string>();
        if (html.Contains("<script", StringComparison.OrdinalIgnoreCase)) problems.Add("<script>");
        if (html.Contains("<link", StringComparison.OrdinalIgnoreCase)) problems.Add("<link>");
        if (html.Contains("@import", StringComparison.OrdinalIgnoreCase)) problems.Add("@import");
        if (RemoteUrl().IsMatch(html)) problems.Add("remote URL");

        return new Check("Self-contained", problems.Count == 0, problems.Count == 0 ? "OK" : string.Join(", ", problems));
    }

    private static Check CheckPageRule(string css)
    {
        var rule = PageRule().Match(css);
        if (!rule.Success) return new Check("@page rule", false, "missing");

        var body = rule.Groups[1].Value;
        var hasMargin = body.Contains("margin", StringComparison.OrdinalIgnoreCase);
        var hasSize = body.Contains("size", StringComparison.OrdinalIgnoreCase);

        return new Check("@page rule", hasMargin, $"size: {(hasSize ? "yes" : "no")}, margin: {(hasMargin ? "yes" : "no")}");
    }

    private static Check CheckPlaceholders(string html)
    {
        var found = Placeholder().Matches(html).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
        var missing = InvoiceFiller.ExpectedPlaceholders.Except(found).ToArray();
        var unknown = found.Except(InvoiceFiller.ExpectedPlaceholders).ToArray();

        var detail = (missing.Length, unknown.Length) switch
        {
            (0, 0) => "all 24 present",
            _ => $"missing: [{string.Join(", ", missing)}] unknown: [{string.Join(", ", unknown)}]",
        };

        return new Check("Placeholders", missing.Length == 0 && unknown.Length == 0, detail);
    }

    private static Check CheckHeaderOverride(string css)
    {
        var match = HeaderDisplayOverride().Match(css);
        return new Check("No thead override", !match.Success, match.Success ? match.Value.Trim() : "OK");
    }

    private static Check CheckTabularNumbers(string css)
    {
        var ok = css.Contains("tabular-nums", StringComparison.OrdinalIgnoreCase);
        return new Check("Tabular numbers", ok, ok ? "OK" : "no font-variant-numeric: tabular-nums");
    }

    /// <summary>
    /// Every page after the first that carries line items must also carry the column labels.
    /// The labels are read from the template's own thead, since each model names them differently.
    /// </summary>
    private static Check CheckHeaderRepeats(string html, string[] pages, Invoice invoice)
    {
        var labels = HeaderCell().Matches(HeadBlock().Match(html).Value)
            .Select(m => Normalize(WebUtility.HtmlDecode(Tags().Replace(m.Groups[1].Value, " "))))
            .Where(l => l.Length > 0)
            .ToArray();

        if (labels.Length == 0) return new Check("Header repeats", false, "no <thead> cells found");

        var pagesWithLines = Enumerable.Range(1, pages.Length - 1)
            .Where(p => invoice.Lines.Any(l => pages[p].Contains(Normalize(l.Sku))))
            .ToArray();

        if (pagesWithLines.Length == 0) return new Check("Header repeats", null, "all line items fit on page 1");

        var failing = pagesWithLines.Where(p => !labels.All(pages[p].Contains)).Select(p => p + 1).ToArray();

        return new Check("Header repeats", failing.Length == 0,
            failing.Length == 0 ? $"on pages {string.Join(", ", pagesWithLines.Select(p => p + 1))}" : $"missing on page {string.Join(", ", failing)}");
    }

    private static Check CheckTotalsTogether(string[] pages, Invoice invoice)
    {
        var culture = InvoiceFiller.CultureFor(invoice);
        var subtotal = PageOf(pages, InvoiceFiller.Money(invoice.Subtotal, culture));
        var total = PageOf(pages, InvoiceFiller.Money(invoice.Total, culture));

        if (subtotal < 0 || total < 0) return new Check("Totals together", false, "subtotal or total not found in the PDF text");

        return new Check("Totals together", subtotal == total, $"subtotal on page {subtotal + 1}, total on page {total + 1}");
    }

    /// <summary>
    /// A row that is sliced by a page break puts its SKU on one page and the rest of it on
    /// the next. The description is the longest cell, so it is the one that gives it away.
    /// </summary>
    private static Check CheckRowsIntact(string[] pages, Invoice invoice)
    {
        // Match SKU and description together. Matching the description alone gives false
        // positives: "Vanilla Syrup 750ml" is also inside "Sugar Free Vanilla Syrup 750ml".
        var broken = invoice.Lines
            .Where(l => l.Sku != StressInvoices.LongRowSku)
            .Where(l =>
            {
                var page = PageOf(pages, l.Sku);
                return page < 0 || !pages[page].Contains(Normalize($"{l.Sku} {l.Description}"));
            })
            .Select(l => l.Sku)
            .ToArray();

        return new Check("Rows intact", broken.Length == 0, broken.Length == 0 ? $"all {invoice.Lines.Count} rows" : $"split: {string.Join(", ", broken.Take(8))}{(broken.Length > 8 ? $" (+{broken.Length - 8} more)" : "")}");
    }

    private static int PageOf(string[] pages, string text) => Array.FindLastIndex(pages, p => p.Contains(Normalize(text)));

    private static string Normalize(string text) => Whitespace().Replace(text, " ").Trim().ToLowerInvariant();

    private static string StripComments(string css) => CssComment().Replace(css, string.Empty);

    /// <summary>
    /// The same settings as the invoice article, so a template that passes here behaves the
    /// same way in that API.
    /// </summary>
    private static ChromePdfRenderer CreateRenderer(bool printBackgrounds = true)
    {
        var renderer = new ChromePdfRenderer();
        var options = renderer.RenderingOptions;

        options.CssMediaType = PdfCssMediaType.Print;
        options.PaperSize = PdfPaperSize.A4;
        options.PrintHtmlBackgrounds = printBackgrounds;
        options.CssPageRulePolicy = CssPageRulePolicy.CssPageWin;
        options.MarginBottom = 16;
        options.UseMarginsOnHeaderAndFooter = UseMargins.All;
        options.EnableJavaScript = false;
        options.TextFooter = new TextHeaderFooter
        {
            LeftText = "Invoice INV-2026-0841",
            RightText = "Page {page} of {total-pages}",
            DrawDividerLine = true,
            FontSize = 8,
        };

        return renderer;
    }

    [GeneratedRegex(@"<style[^>]*>(.*?)</style>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex StyleBlock();

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex CssComment();

    [GeneratedRegex(@"@page[^{]*\{([^}]*)\}", RegexOptions.IgnoreCase)]
    private static partial Regex PageRule();

    [GeneratedRegex(@"\{\{\s*(\w+)\s*\}\}")]
    private static partial Regex Placeholder();

    // The lookbehind stops ".masthead" from matching, which every invoice template has.
    [GeneratedRegex(@"(?<![\w-])thead\b[^{}]*\{[^}]*display\s*:\s*(block|flex|grid|inline-block|inline)\b", RegexOptions.IgnoreCase)]
    private static partial Regex HeaderDisplayOverride();

    [GeneratedRegex(@"<thead[^>]*>.*?</thead>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex HeadBlock();

    [GeneratedRegex(@"<th[^>]*>(.*?)</th>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex HeaderCell();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex Tags();

    [GeneratedRegex(@"(src|href)\s*=\s*[""']?https?://|url\(\s*[""']?https?://", RegexOptions.IgnoreCase)]
    private static partial Regex RemoteUrl();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
