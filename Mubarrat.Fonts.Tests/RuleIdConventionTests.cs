using Mubarrat.Fonts.OpenType.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Mubarrat.Fonts.Tests;

public partial class RuleIdConventionTests
{
    // OT.<table>.<slug> or OT.cross.<a>[-<b>][-<c>]
    [GeneratedRegex(@"^OT\.([a-z0-9_]+|cross)\.([a-z0-9]+(-[a-z0-9]+)*)$", RegexOptions.Compiled)]
    private static partial Regex Pattern { get; }

    [Fact]
    public void AllRuleIdsFollowConvention()
    {
        var descriptors = typeof(DiagnosticDescriptor).Assembly
            .GetTypes()
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(f => f.FieldType == typeof(DiagnosticDescriptor))
            .Select(f => (DiagnosticDescriptor)f.GetValue(null)!)
            .ToList();

        Assert.NotEmpty(descriptors);

        var seen = new Dictionary<string, string>();
        foreach (var d in descriptors)
        {
            Assert.Matches(Pattern, d.Id);

            if (seen.TryGetValue(d.Id, out var other))
                Assert.Fail($"Duplicate rule ID '{d.Id}' used by {other} and {d.Title}");
            seen[d.Id] = d.Title;
        }
    }
}
