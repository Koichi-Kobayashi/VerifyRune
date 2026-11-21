using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using KanjiVariants;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine(RuntimeInformation.FrameworkDescription);

ExpectOutput("scalar counts", Example1, new[] {
    "UTF-16: 1 / スカラー値: U+9AD8",
    "UTF-16: 2 / スカラー値: U+20BB7",
    "UTF-16: 3 / スカラー値: U+8FBB U+E0100"
});
ExpectOutput("KanjiCharacter and KanjiText", Example2, new[] { "U+8FBB / U+E0100", "辻", "高 U+D800 吉" });
ExpectOutput("append comparison", Example4, new[] { "True" });

Expression<Func<StringBuilder, Rune, StringBuilder>> append = (builder, rune) => builder.Append(rune);
var method = ((MethodCallExpression)append.Body).Method;
#if NET11_0_OR_GREATER
Check(method.GetParameters().Single().ParameterType == typeof(Rune), "Append(Rune) binding");
ExpectOutput("Rune search", Example3, new[] { "True", "True", "3" });
ExpectOutput("position and replacement flag", Example5, new[] {
    "0 2 U+20BB7 False", "2 1 U+8FBB False", "3 2 U+E0100 False", "5 1 U+FFFD True", "6 1 U+FFFD False"
});
#else
Check(method.GetParameters().Single().ParameterType == typeof(object), "Append(object) binding");
#endif
Console.WriteLine($"Bound overload: {method}");

const string ivs = "辻\U000E0100";
var input = "𠮷" + ivs;
var legacy = new StringBuilder();
var direct = new StringBuilder();
var appendRune = append.Compile();
var appendLegacy = typeof(KanjiText)
    .GetMethod("AppendRune", BindingFlags.Static | BindingFlags.NonPublic)!
    .CreateDelegate<Action<StringBuilder, int>>();
foreach (var rune in input.EnumerateRunes())
{
    appendLegacy(legacy, rune.Value);
    appendRune(direct, rune);
}
Check(legacy.ToString() == input && direct.ToString() == input, "Both append paths preserve the original code units");
Check(KanjiCharacter.Parse("𠮷").BaseCharacter.Value == 0x20BB7, "Supplementary scalar parsing");
Check(KanjiCharacter.Parse(ivs).ToString() == ivs, "IVS round trip");
Check(!KanjiCharacter.TryParse("\uD800", out _), "Isolated surrogate is rejected");
Check(!KanjiCharacter.TryParse("髙\uFE0F", out _), "Unregistered variation sequence is rejected");

var set = CharacterSet.JisX0208;
Check(KanjiText.Replace(ivs, set) == ivs, "IVS preserved without fallback");
Check(KanjiText.Replace(ivs, set, KanjiFallbackOptions.AllowVariationSelectorFallback) == "辻", "IVS fallback");
Check(!KanjiText.IsSupported("髙\uD800𠮷", set), "Invalid UTF-16 is unsupported");
Check(KanjiText.Replace("髙\uD800𠮷", set) == "高\uD800吉", "Invalid UTF-16 preserved while replacing on both sides");
var invalid = KanjiDiagnostics.AnalyzeText("𠮷\uD800辻", set).Items[1];
Check(invalid.Reason == KanjiDiagnosticReason.InvalidUtf16 && invalid.Original == "\uD800" &&
    invalid.Utf16Index == 2 && invalid.Utf16Length == 1, "Diagnostic retains the invalid code unit and its position");

static void Check(bool condition, string description)
{
    if (!condition)
        throw new InvalidOperationException(description);
    Console.WriteLine($"PASS {description}");
}

static void ExpectOutput(string name, Action sample, string[] expected)
{
    TextWriter originalOutput = Console.Out;
    using var output = new StringWriter();
    try
    {
        Console.SetOut(output);
        sample();
    }
    finally
    {
        Console.SetOut(originalOutput);
    }

    string expectedOutput = string.Join(Environment.NewLine, expected) + Environment.NewLine;
    Check(output.ToString() == expectedOutput, $"{name} (actual: {output.ToString().TrimEnd().Replace(Environment.NewLine, " | ")})");
}
static void Example1() {

foreach (string text in new[] { "高", "𠮷", "辻\U000E0100" })
{
    Console.Write($"UTF-16: {text.Length} / スカラー値:");
    foreach (Rune rune in text.EnumerateRunes())
        Console.Write($" U+{rune.Value:X4}");
    Console.WriteLine();
}
}
static void Example2() {

const string ivs = "辻\U000E0100";
var kanji = KanjiCharacter.Parse(ivs);
Console.WriteLine($"U+{kanji.BaseCharacter.Value:X4} / U+{kanji.VariationSelector?.Value:X4}");
// U+8FBB / U+E0100

var set = CharacterSet.JisX0208;
Console.WriteLine(KanjiText.Replace(ivs, set, KanjiFallbackOptions.AllowVariationSelectorFallback));
// 辻

string replaced = KanjiText.Replace("髙\uD800𠮷", set);
Console.WriteLine($"{replaced[0]} U+{(int)replaced[1]:X4} {replaced[2]}");
// 高 U+D800 吉
}
#if NET11_0_OR_GREATER
static void Example3() {

var yoshi = new Rune(0x20BB7);
string text = "A𠮷野";

Console.WriteLine(text.Contains(yoshi.ToString(), StringComparison.Ordinal)); // True。従来から可能
Console.WriteLine(text.Contains(yoshi));                                    // True。.NET 11
Console.WriteLine(text.IndexOf(new Rune('野')));                             // 3。UTF-16 の位置
}
#endif
static void Example4() {

var legacy = new StringBuilder();
var direct = new StringBuilder();
foreach (Rune rune in "𠮷辻\U000E0100".EnumerateRunes())
{
    AppendRune(legacy, rune.Value);
    direct.Append(rune);
}
Console.WriteLine(legacy.ToString() == direct.ToString()); // True

static void AppendRune(StringBuilder builder, int codePoint)
{
    Span<char> buffer = stackalloc char[2];
    int written = new Rune(codePoint).EncodeToUtf16(buffer);
    builder.Append(buffer[..written]);
}
}
#if NET11_0_OR_GREATER
static void Example5() {

string text = "𠮷辻\U000E0100\uD800\uFFFD";
foreach (var item in RunePosition.EnumerateUtf16(text.AsSpan()))
    Console.WriteLine($"{item.StartIndex} {item.Length} U+{item.Rune.Value:X4} {item.WasReplaced}");
}
#endif
