using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using TemplateScorer.Scoring;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

IronPdf.License.LicenseKey = config["IronPdf:LicenseKey"]
    ?? throw new InvalidOperationException("Set IronPdf:LicenseKey in user secrets or as IronPdf__LicenseKey.");

// runs/<model>/<run>.html in, out/<model>/<run>.pdf + page 2 PNG out.
var runsFolder = Path.GetFullPath(args.ElementAtOrDefault(0) ?? "../runs");
var outFolder = Path.GetFullPath(args.ElementAtOrDefault(1) ?? "../out");

var scorer = new TemplateScorer.Scoring.TemplateScorer();
var cards = new List<ScoreCard>();

foreach (var modelFolder in Directory.GetDirectories(runsFolder).Order())
{
    var model = Path.GetFileName(modelFolder);
    foreach (var template in Directory.GetFiles(modelFolder, "*.html").Order())
    {
        var card = await scorer.ScoreAsync(model, template, Path.Combine(outFolder, model));
        cards.Add(card);

        Console.WriteLine($"{model}/{card.Run}: {card.Score}/{card.OutOf}, {card.PageCount} pages");
        foreach (var check in card.Checks.Where(c => c.Passed != true))
        {
            Console.WriteLine($"    {(check.Passed is null ? "n/a " : "FAIL")} {check.Name}: {check.Detail}");
        }
    }
}

// Stress round: same templates, harder input. Results go to out/stress-<test>/.
var stress = new StringBuilder();
var stressCards = new Dictionary<string, List<ScoreCard>>();
foreach (var test in Enum.GetValues<StressTest>())
{
    var testCards = new List<ScoreCard>();
    foreach (var modelFolder in Directory.GetDirectories(runsFolder).Order())
    {
        var model = Path.GetFileName(modelFolder);
        foreach (var template in Directory.GetFiles(modelFolder, "r1-*.html").Concat(Directory.GetFiles(modelFolder, "*template*.html")).Concat(Directory.GetFiles(modelFolder, "control-*.html")).Order())
        {
            var card = await scorer.StressAsync(test, model, template, Path.Combine(outFolder, $"stress-{test}", model));
            testCards.Add(card);
            Console.WriteLine($"[{test}] {model}/{card.Run}: {card.Score}/{card.OutOf}, {card.PageCount} pages");
            foreach (var check in card.Checks.Where(c => c.Passed != true))
            {
                Console.WriteLine($"    {(check.Passed is null ? "n/a " : "FAIL")} {check.Name}: {check.Detail}");
            }
        }
    }

    stressCards[test.ToString()] = testCards;
    stress.AppendLine($"## {test}").AppendLine().AppendLine(ToMarkdown(testCards, withDetail: true));
}

Directory.CreateDirectory(outFolder);
await File.WriteAllTextAsync(Path.Combine(outFolder, "results-stress.md"), stress.ToString());
await File.WriteAllTextAsync(Path.Combine(outFolder, "results-stress.json"),
    JsonSerializer.Serialize(stressCards, new JsonSerializerOptions { WriteIndented = true }));
await File.WriteAllTextAsync(Path.Combine(outFolder, "results.json"),
    JsonSerializer.Serialize(cards, new JsonSerializerOptions { WriteIndented = true }));
await File.WriteAllTextAsync(Path.Combine(outFolder, "results.md"), ToMarkdown(cards));  // written last: score.sh waits on it

Console.WriteLine($"Wrote {cards.Count} score cards to {outFolder}");

static string ToMarkdown(List<ScoreCard> cards, bool withDetail = false)
{
    if (cards.Count == 0) return "No runs found.";

    var names = cards[0].Checks.Select(c => c.Name).ToArray();
    var md = new StringBuilder();
    md.AppendLine($"| Model | Run | Pages | Score | {string.Join(" | ", names)} |");
    md.AppendLine($"|---|---|---|---|{string.Concat(names.Select(_ => "---|"))}");

    foreach (var card in cards)
    {
        var cells = names.Select(n => card.Checks.FirstOrDefault(c => c.Name == n) switch
        {
            null => "-",
            { Passed: true } c => withDetail ? $"pass ({c.Detail})" : "pass",
            { Passed: false } c => withDetail ? $"FAIL ({c.Detail})" : "FAIL",
            _ => "n/a",
        });
        md.AppendLine($"| {card.Model} | {card.Run} | {card.PageCount} | {card.Score}/{card.OutOf} | {string.Join(" | ", cells)} |");
    }

    return md.ToString();
}
