using System.Text.RegularExpressions;
using Xunit;

namespace VenueIQ.Tests.Localization;

public class LocalizationGuardTests
{
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    private static readonly string AppSrc = Path.Combine(RepoRoot, "src", "VenueIQ.App");

    [Fact]
    public void Xaml_Should_Not_Contain_Hardcoded_Text()
    {
        var xamlFiles = Directory.GetFiles(AppSrc, "*.xaml", SearchOption.AllDirectories);
        var offenders = new List<(string file, int line, string text)>();
        var attrRe = new Regex("\n\s*[A-Za-z:]*?(Text|Title|Placeholder|SemanticProperties.Description)\\s*=\\\"([^\\\"]+)\\\"", RegexOptions.Compiled);
        var allowedLiterals = new HashSet<string> { "i", "★", "•", "✓", "!" };
        foreach (var f in xamlFiles)
        {
            var lines = File.ReadAllLines(f);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var m = attrRe.Match("\n" + line);
                if (m.Success)
                {
                    var val = m.Groups[2].Value;
                    if (val.Contains("{")) continue; // binding or markup extension
                    if (allowedLiterals.Contains(val)) continue;
                    if (val.StartsWith("Results.Item.")) continue; // automation ids
                    // Offender
                    offenders.Add((f, i + 1, val));
                }
            }
        }
        Assert.True(offenders.Count == 0, "Hardcoded XAML text found:\n" + string.Join("\n", offenders.Select(o => $"{o.file}:{o.line} -> {o.text}")));
    }

    [Fact]
    public void Cs_Should_Not_Set_Text_Properties_With_Literals()
    {
        var csFiles = Directory.GetFiles(AppSrc, "*.cs", SearchOption.AllDirectories);
        var offenders = new List<(string file, int line, string text)>();
        var propRe = new Regex("\\.Text\\s*=\\s*\"([^\"]+)\"|Announce\\(\\s*\"([^\"]+)\"", RegexOptions.Compiled);
        foreach (var f in csFiles)
        {
            var lines = File.ReadAllLines(f);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var m = propRe.Match(line);
                if (m.Success)
                {
                    var val = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
                    // allow empty and icons
                    if (string.IsNullOrWhiteSpace(val)) continue;
                    if (val.Length <= 2 && new[]{"i","★","•","✓","!"}.Contains(val)) continue;
                    offenders.Add((f, i + 1, val));
                }
            }
        }
        Assert.True(offenders.Count == 0, "Hardcoded C# UI text found:\n" + string.Join("\n", offenders.Select(o => $"{o.file}:{o.line} -> {o.text}")));
    }
}

