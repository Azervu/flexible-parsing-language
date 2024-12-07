using FlexibleParsingLanguage.Compiler;

namespace FlexibleParsingLanguage.Test;

[TestClass]
public class TokenizerTest
{
    private Tokenizer T { get; set; } = new Lexicalizer().Tokenizer;

    public static IEnumerable<object[]> TokenizationData => new List<object[]>
    {
        new object[] { "'-\\'-'|t.aaa*bbb|{ccc}\" \"", new List<(char, string?)> { ('\'', "-'-"), ('|', "t"), ('.', "aaa"), ('*', ""), ('.', "bbb"), ('|', ""), ('{', ""), ('.', "ccc"), ('}', ""), ('"', " ") } },
        new object[] { "|json*id@", new List<(char, string?)> { ('|', "json"), ('*', ""), ('.', "id"), ('@', "") } },
    };


    [TestMethod]
    [DynamicData(nameof(TokenizationData))]
    public void QueryParserTest(string parserString, List<(char, string?)> excpectedResult) => TestUtil(parserString, excpectedResult);


    public void TestUtil(string parserString, List<(char, string?)> excpectedResult)
    {
        var parsed = T.Tokenize(parserString);

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

    }
}