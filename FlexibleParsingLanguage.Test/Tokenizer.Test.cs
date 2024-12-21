using FlexibleParsingLanguage.Compiler.Util;
using System.Text;

namespace FlexibleParsingLanguage.Test;

[TestClass]
public class TokenizerTest
{
    private Lexicalizer T { get; set; } = new Compiler.Compiler().Lexicalizer;






    public static IEnumerable<object[]> NextTokenizationData => new List<object[]>
    {
        new object[] { "a.b#c.d", @"
{
    .  ""a""
    .  ""b""
.  ""c""
|
    {
        .  ""d""
        .  ""e""
        .  ""f""
.  ""g""" },
        new object[] { "'-\\'-'|t.aaa*bbb##gfjhd|{ccc}\" \"", @"
'  ""-'-""
|  ""t""
.  ""aaa""
*
.  ""bbb""
##  ""gfjhd""
|
    {
        .  ""ccc""
"" """ },
        new object[] { "k:h", @"
.  ""k""
:  ""h""" },
    };



    [TestMethod]
    [DynamicData(nameof(NextTokenizationData))]
    public void QueryParserTest2(string parserString, string excpectedResult) => TestUtil2(parserString, excpectedResult);




    public void TestUtil2(string parserString, string excpectedResult)
    {
        var (_, parsed2) = T.Lexicalize(parserString);
        var log = new StringBuilder("\n");

        var proccessed = new HashSet<int>();

        foreach (var t in parsed2)
            LogEntry(proccessed, log, t);

        var result = log.ToString().Replace("\r", string.Empty);
        var expected = excpectedResult.Replace("\r", string.Empty);
        Assert.AreEqual(expected, result, "string rep mismatch");
    }

    private void LogEntry(HashSet<int> proccessed, StringBuilder log, RawOp t)
    {
        if (proccessed.Contains(t.Id))
            return;
        foreach (var input in t.Input)
            LogEntry(proccessed, log, input);
        log.AppendLine($"{t.Id} {(t.Type.Operator ?? string.Empty).PadLeft(2)} [{t.Input.Select(x => x.Id.ToString()).Join(",")}/{t.Output.Select(x => x.Id.ToString()).Join(",")}] '{t.Accessor}'");
    }














    public static IEnumerable<object[]> TokenizationData => new List<object[]>
    {
        new object[] { "{a.b}c|{d.e.f}g", @"
{
    .  ""a""
    .  ""b""
.  ""c""
|
    {
        .  ""d""
        .  ""e""
        .  ""f""
.  ""g""" },
        new object[] { "'-\\'-'|t.aaa*bbb##gfjhd|{ccc}\" \"", @"
'  ""-'-""
|  ""t""
.  ""aaa""
*
.  ""bbb""
##  ""gfjhd""
|
    {
        .  ""ccc""
"" """ },
        new object[] { "k:h", @"
.  ""k""
:  ""h""" },
    };





    [TestMethod]
    [DynamicData(nameof(TokenizationData))]
    public void QueryParserTest(string parserString, string excpectedResult) => TestUtil(parserString, excpectedResult);


    public void TestUtil(string parserString, string excpectedResult)
    {
        var (parsed, parsed2) = T.Lexicalize(parserString);
        var result = parsed.Select(x => x.ToString2()).Concat().Replace("\r", string.Empty);




        var expected = excpectedResult.Replace("\r", string.Empty);

        /*
        var e = expected.Split('\n');
        var r = result.Split('\n');
        for (var i = 0; i < e.Length; i++)
        {
            var ee = e[i];
            var rr = r[i];
            Assert.AreEqual(rr, ee, $"failed parsing {parserString}");
        }
        */


        Assert.AreEqual(expected, result, "string rep mismatch");




        /*
        Assert.AreEqual(excpectedResult.Count, parsed.Count, $"length mismatch | \n{excpectedResult.Select(x => $"{x.Item1}{x.Item2}").Join(' ')} \n{parsed.Select(x => $"{x.Item1}{x.Item2}").Join(' ')}");

        */

    }


}