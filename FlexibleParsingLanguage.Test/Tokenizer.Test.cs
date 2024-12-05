using FlexibleParsingLanguage.Compiler;

namespace FlexibleParsingLanguage.Test;

[TestClass]
public class TokenizerTest
{
    private Tokenizer T { get; set; } = new Lexicalizer().Tokenizer;


    [TestMethod]
    public void QueryParserTest() => TestUtil("'-\\'-'|t.aaa*bbb|{ccc}\" \"", [('\'', "-'-"), ('|', "t"), ('.', "aaa"), ('*', null), ('.', "bbb"), ('|', null), ('{', null), ('.', "ccc"), ('}', null), ('"', " ")]);


    public void TestUtil(string parserString, List<(char, string?)> excpectedResult)
    {
        var parsed = T.Tokenize(parserString);

        Assert.AreEqual(excpectedResult.Count, parsed.Count, $"length mismatch | \n{string.Concat(excpectedResult.Select(x => $"{x.Item1}{x.Item2}"))} \n{string.Concat(parsed.Select(x => $"{x.Item1}{x.Item2}"))}");

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