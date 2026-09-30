using System.Globalization;
using System.Net;
using System.Text;

namespace TemplateScorer.Invoices;

/// <summary>
/// Fills a template with the sample invoice. Same logic as the InvoiceHtmlBuilder in the
/// invoice article, except the template comes in as a string, because here it is whatever
/// the model handed back.
/// </summary>
public static class InvoiceFiller
{
    /// <summary>
    /// The placeholder contract given to both models in the prompt. A template that uses
    /// anything else cannot be filled by this code, which is exactly what the scorer checks.
    /// </summary>
    public static readonly IReadOnlySet<string> ExpectedPlaceholders = new HashSet<string>(StringComparer.Ordinal)
    {
        "Number", "IssuedOn", "DueOn", "PaymentTerms", "Lines",
        "Subtotal", "TotalDiscount", "Tax", "TaxRatePercent", "Total",
        "SellerName", "SellerAddress1", "SellerAddress2", "SellerCity", "SellerPostCode", "SellerCountry", "SellerTaxId",
        "BuyerName", "BuyerAddress1", "BuyerAddress2", "BuyerCity", "BuyerPostCode", "BuyerCountry", "BuyerTaxId",
    };

    /// <summary>
    /// Money is formatted for the invoice currency, never the server culture. Same rule as
    /// the invoice article, trimmed to the two currencies the stress tests use.
    /// </summary>
    public static CultureInfo CultureFor(Invoice invoice) => invoice.CurrencyCode switch
    {
        "EUR" => CultureInfo.GetCultureInfo("de-DE"),
        _ => CultureInfo.GetCultureInfo("en-GB"),
    };

    public static string Money(decimal value, CultureInfo culture) => value.ToString("C", culture);

    public static string Fill(string template, Invoice invoice)
    {
        var culture = CultureFor(invoice);
        string Money(decimal value) => InvoiceFiller.Money(value, culture);
        string Date(DateOnly value) => value.ToString("dd MMM yyyy", culture);

        var rows = new StringBuilder();
        foreach (var line in invoice.Lines)
        {
            rows.Append(
                $"""
                     <tr>
                       <td class="sku">{Encode(line.Sku)}</td>
                       <td>{Encode(line.Description)}</td>
                       <td class="num">{line.Quantity.ToString("N0", culture)}</td>
                       <td class="num">{Money(line.UnitPrice)}</td>
                       <td class="num">{line.DiscountRate.ToString("P0", culture)}</td>
                       <td class="num">{Money(line.NetAmount)}</td>
                     </tr>

                 """);
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Number"] = Encode(invoice.Number),
            ["IssuedOn"] = Date(invoice.IssuedOn),
            ["DueOn"] = Date(invoice.DueOn),
            ["PaymentTerms"] = Encode(invoice.PaymentTerms),
            ["Lines"] = rows.ToString(),
            ["Subtotal"] = Money(invoice.Subtotal),
            ["TotalDiscount"] = Money(invoice.TotalDiscount),
            ["Tax"] = Money(invoice.Tax),
            ["Total"] = Money(invoice.Total),
            ["TaxRatePercent"] = invoice.TaxRate.ToString("P0", culture),
        };

        AddParty(values, "Seller", invoice.Seller);
        AddParty(values, "Buyer", invoice.Buyer);

        var html = new StringBuilder(template);
        foreach (var (key, value) in values)
        {
            html.Replace($"{{{{{key}}}}}", value);
        }

        return html.ToString();
    }

    private static void AddParty(Dictionary<string, string> values, string prefix, Party party)
    {
        values[$"{prefix}Name"] = Encode(party.Name);
        values[$"{prefix}Address1"] = Encode(party.AddressLine1);
        values[$"{prefix}Address2"] = Encode(party.AddressLine2);
        values[$"{prefix}City"] = Encode(party.City);
        values[$"{prefix}PostCode"] = Encode(party.PostCode);
        values[$"{prefix}Country"] = Encode(party.Country);
        values[$"{prefix}TaxId"] = Encode(party.TaxId);
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
