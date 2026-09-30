# Claude Fable 5.1 vs GPT-6 Astra: HTML to PDF in C# (.NET 10)

Source code for the article [Claude Fable 5.1 vs GPT-6 Astra: HTML to PDF in C#](https://codewithmukesh.com/blog/claude-vs-chatgpt-html-to-pdf-csharp/) on codewithmukesh.com.

I gave both models the same invoice prompt, rendered every template with IronPDF on .NET 10, and scored the PDFs in C# by extracting the text of each page.

## What is in here

- `TemplateScorer/` - the .NET 10 console app that renders and scores every template
- `runs/` - the raw HTML each model returned, saved exactly as it came back (5 runs per model), plus the baseline and known-broken control templates
- `out/` - the rendered PDFs, page 2 screenshots, and the results (`results.md`, `results-stress.md` and the JSON versions)

## How the runs were done

- Claude Fable 5.1 in the Claude app (Max 5x plan), GPT-6 Astra in the ChatGPT app (Plus plan, Medium, the default)
- A new chat for every run, default settings, no follow-up prompts
- The HTML was saved as-is, without fixing anything

The prompt:

```text
Write a single-file HTML invoice template for A4 print output.
CSS Grid for the masthead and the seller/buyer blocks. One <table> for
line items with columns: SKU, description, qty, unit price, discount,
amount. A totals block with subtotal, discount, tax, and grand total.
Neutral professional styling, no external assets, no JavaScript.

Use exactly these placeholders and no others:
{{Number}} {{IssuedOn}} {{DueOn}} {{PaymentTerms}}
{{SellerName}} {{SellerAddress1}} {{SellerAddress2}} {{SellerCity}} {{SellerPostCode}} {{SellerCountry}} {{SellerTaxId}}
{{BuyerName}} {{BuyerAddress1}} {{BuyerAddress2}} {{BuyerCity}} {{BuyerPostCode}} {{BuyerCountry}} {{BuyerTaxId}}
{{Subtotal}} {{TotalDiscount}} {{Tax}} {{TaxRatePercent}} {{Total}}

{{Lines}} goes inside the <tbody> and gets replaced with one <tr> per
line item, shaped like this:
<tr><td class="sku">..</td><td>..</td><td class="num">..</td><td class="num">..</td><td class="num">..</td><td class="num">..</td></tr>

Return only the HTML.
```

## Run it yourself

You need the .NET 10 SDK and an IronPDF license key. A [free trial key](https://ironpdf.com/get-started/license-keys/#trial-license) works.

```bash
cd TemplateScorer
dotnet user-secrets set "IronPdf:LicenseKey" "<your trial key>"
dotnet run
```

The scorer re-renders every template in `runs/`, runs the stress round (backgrounds off, a long row at the page break, 200 lines, euro amounts with long names) and writes the results to `out/`.

A sandbox key only works with a debugger attached, so with one of those, open `TemplateScorer.slnx` in Visual Studio or Rider and press F5 instead of using `dotnet run`.

Happy Coding :)
