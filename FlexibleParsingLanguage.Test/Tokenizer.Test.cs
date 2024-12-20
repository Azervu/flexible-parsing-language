using FlexibleParsingLanguage.Compiler.Util;
using System.Text;

namespace FlexibleParsingLanguage.Test;

[TestClass]
public class TokenizerTest
{
    private Lexicalizer T { get; set; } = new Compiler.Compiler().Lexicalizer;

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
        var parsed = T.Lexicalize(parserString);
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