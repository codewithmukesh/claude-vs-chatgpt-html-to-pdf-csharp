namespace TemplateScorer.Invoices;

/// <summary>
/// Harder variations of the sample invoice. The 46 line order from the invoice article
/// only reaches page three and every row fits on one line, which both models handled
/// without trouble. These push on the parts of a print template that break later.
/// </summary>
public static class StressInvoices
{
    /// <summary>A row long enough to wrap onto several lines. The first and last words are
    /// unique across the invoice, so the scorer can tell which page each end landed on.</summary>
    public const string LongDescription =
        "Autumn launch bundle for new cafe openings: six cold brew concentrates, four oat milk " +
        "barista cartons, two vanilla syrups, branded cups and lids for five hundred servings, " +
        "laminated menu cards, window decals and a printed barista starter guide";

    public const string LongRowSku = "PR-9001";

    /// <summary>
    /// Inserts the long row at a given position. The scorer picks the position per template,
    /// just before the last row that fits on page one, so the row straddles the page break.
    /// </summary>
    public static Invoice WithLongRow(Invoice invoice, int index)
    {
        var lines = invoice.Lines.ToList();
        lines.Insert(index, new InvoiceLine(LongRowSku, LongDescription, 3, 189.00m, 0.10m));
        return invoice with { Lines = lines };
    }

    /// <summary>
    /// 200 lines, roughly seven or eight pages. SKUs are fixed width (LN-0001 to LN-0200)
    /// so no SKU is a substring of another one, which would confuse the page lookups.
    /// </summary>
    public static Invoice WithManyLines(Invoice invoice, int count = 200)
    {
        var source = invoice.Lines;
        var lines = Enumerable.Range(0, count)
            .Select(i => source[i % source.Count] with { Sku = $"LN-{i + 1:D4}" })
            .ToArray();

        return invoice with { Lines = lines };
    }

    /// <summary>
    /// Euro amounts in German formatting (1.234,56 €) and names long enough to test the
    /// seller and buyer grid. The currency symbol moves to the end and every amount gets longer.
    /// </summary>
    public static Invoice InEurosWithLongNames(Invoice invoice) => invoice with
    {
        CurrencyCode = "EUR",
        Buyer = invoice.Buyer with
        {
            Name = "Kaffeeroesterei am Rheinufer Handels- und Vertriebsgesellschaft mbH",
            AddressLine1 = "Hafenstrasse 117, Gebaeude C, Lieferanteneingang Rueckseite",
            City = "Duesseldorf",
            PostCode = "40213",
            Country = "Germany",
            TaxId = "DE 318 774 902",
        },
    };
}
