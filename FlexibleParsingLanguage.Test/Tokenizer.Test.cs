using FlexibleParsingLanguage.Compiler;
using System.Text;

namespace FlexibleParsingLanguage.Test;

[TestClass]
public class TokenizerTest
{
    private Lexicalizer T { get; set; } = new Compiler.Compiler().Tokenizer;

    public static IEnumerable<object[]> TokenizationData => new List<object[]>
    {
        new object[] { "{a.b}c|{d.e.f}g", @"
{
    {
        .  ""a""
        .  ""b""
    .  ""c""
    |
        {
            .  ""d""
            .  ""e""
            .  ""f""
    .  ""g""
" },
        new object[] { "'-\\'-'|t.aaa*bbb€€gfjhd|{ccc}\" \"", @"
{
    '  ""-'-""
    |  ""t""
    .  ""aaa""
    *  ""bbb""
    €€  ""gfjhd""
    |
        {
            .  ""ccc""
    ""  "" ""
" },
        new object[] { "k:h", @"
{
    .  ""k""
    :  ""h""
" },












        
    };





    [TestMethod]
    [DynamicData(nameof(TokenizationData))]
    public void QueryParserTest(string parserString, string excpectedResult) => TestUtil(parserString, excpectedResult);


    public void TestUtil(string parserString, string excpectedResult)
    {
        var parsed = T.Lexicalize(parserString);
        var result = parsed.ToString2().Replace("\r", string.Empty);


        var expected = excpectedResult.Replace("\r", string.Empty);



        Assert.AreEqual(expected, result, "string rep mismatch");




        /*
        Assert.AreEqual(excpectedResult.Count, parsed.Count, $"length mismatch | \n{excpectedResult.Select(x => $"{x.Item1}{x.Item2}").Join(' ')} \n{parsed.Select(x => $"{x.Item1}{x.Item2}").Join(' ')}");

        for (var i = 0; i < parsed.Count; i++)
        {
            var expected = excpectedResult[i];
            var actual = parsed[i];


            if (actual != expected)
            {


                var s = 345354;
            }

            Assert.AreEqual(expected, actual, $"failed parsing {parserString}");
        }
        */

    }
}