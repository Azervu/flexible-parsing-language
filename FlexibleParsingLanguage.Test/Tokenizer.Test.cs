namespace FlexibleParsingLanguage.Test;

[TestClass]
public class TokenizerTest
{
    private Tokenizer T { get; set; } = new Lexicalizer().Tokenizer;


    [TestMethod]
    public void QueryParserTest() => TestUtil("'-\\'-'aaa*bbb{ccc}\" \"", [('\'', "-'-"), ('.', "aaa"), ('*', null), ('.', "bbb"), ('{', null), ('.', "ccc"), ('}', null), ('"', " ")]);


    public void TestUtil(string parserString, List<(char, string?)> excpectedResult)
    {
        var parsed = T.Tokenize(parserString);

        Assert.AreEqual(excpectedResult.Count, parsed.Count, $"length mismatch | {string.Concat(excpectedResult.Select(x => $"{x.Item1}{x.Item2}"))} {string.Concat(parsed.Select(x => $"{x.Item1}{x.Item2}"))}");

        for (var i = 0; i < parsed.Count; i++)
            Assert.AreEqual(excpectedResult[i], parsed[i], $"failed parsing {parserString}");

    }
}