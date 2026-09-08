using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DatabaseFinder;

/// <summary>Application-owned text only. Paths, queries and database names are never translated.</summary>
public static class L
{
    private static readonly Dictionary<string, string> Persian = Load("fa");
    private static readonly Dictionary<string, string> English = Load("en");
    public static string Language { get; set; } = "fa";
    public static bool IsFa => Language != "en";
    public static string Text(string key) => (IsFa ? Persian : English).TryGetValue(key, out var value) ? value : throw new KeyNotFoundException(key);
    public static string Pick(string fa, string en) => IsFa ? fa : en;
    public static string DisplayFormat(string value)
    {
        foreach (var key in new[] { "S200", "S210", "S212", "S213", "S214" })
            if (value == Persian[key] || value == English[key]) return Text(key);
        return value;
    }
    public static string Format(string key, params object?[] args)
    {
        var index = 0;
        return Regex.Replace(Text(key), @"\{#\}", _ => Convert.ToString(args[index++], CultureInfo.CurrentCulture) ?? "");
    }
    private static Dictionary<string, string> Load(string language)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"DatabaseFinder.Strings.{language}.json")
            ?? throw new InvalidOperationException("Missing translation resource: " + language);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
    }
}
