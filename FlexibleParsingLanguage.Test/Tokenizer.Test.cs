using FlexibleParsingLanguage.Compiler.Util;
using System.Text;

namespace FlexibleParsingLanguage.Test;

[TestClass]
public class TokenizerTest
{
    private Lexicalizer T { get; set; } = new Compiler.Compiler().Lexicalizer;

    public static IEnumerable<object[]> ValidQueries => new List<object[]>
    {

        new object[] {"Simple", "a.b#c.d", "1{  3'a'  2.[1,3]  5'b'  4.[2,5]  7'c'  6#[4,7]"},
        new object[] {"Redundant Separator", "b.#c",  "1{  2'a'  3.[1,2]  4'b'  5.[3,4]  6'c'  7#[5,6]  8'd'  9.[7,8]"},
        new object[] {"Escape", "a.b'ee\\'e'c.d", "1{  3'a'  2.[1,3]  5'b'  4.[2,5]  7'''  6.[4,7]  9'c'  8.[6,9]"},
    };

    public static IEnumerable<object[]> InvalidQueries => new List<object[]>
    {
        new object[] { "Un ended escape", "a.b'sdf" },
    };

    [TestMethod]
    [DynamicData(nameof(ValidQueries))]
    public void QueryParserTest2(string namme, string query, string expectedResult) => TestUtil2(query, expectedResult);

    public void TestUtil2(string parserString, string excpectedResult)
    {

        List<RawOp> parsed = new List<RawOp>();
        try
        {
            parsed = T.Lexicalize(parserString);
        }
        catch (QueryCompileException ex)
        {
            Assert.Fail(ex.GenerateMessage(parserString));
        }

        var log = new StringBuilder();

        var proccessed = new HashSet<int>();

        foreach (var t in parsed)
            LogEntry(proccessed, log, t);

        var result = log.ToString().Replace("\r", string.Empty);
        var expected = excpectedResult.Replace("\r", string.Empty);

        if (expected != result)
        {
            Assert.Fail($"Query mismatch {parserString}\n{expected}\n{result}");
        }

        Assert.AreEqual(expected, result, "string rep mismatch");
    }

    [TestMethod]
    [DynamicData(nameof(InvalidQueries))]
    public void CatchInvalidQueryTest(string name, string query)
    {
        try
        {
            var parsed = T.Lexicalize(query);
 
        }
        catch (QueryCompileException ex)
        {
            //only QueryCompileException should be thrown - otherwise it's a library issue
            return;
        }
        Assert.Fail($"Failed to catch issue in {query}");
    }












    private void LogEntry(HashSet<int> proccessed, StringBuilder log, RawOp t)
    {
        if (proccessed.Contains(t.Id))
            return;
        foreach (var input in t.Input)
            LogEntry(proccessed, log, input);

        if (log.Length > 0)
            log.Append("  ");


        proccessed.Add(t.Id);

        log.Append($"{t.Id}");

        if (!string.IsNullOrEmpty(t.Type.Operator))
            log.Append(t.Type.Operator);


        if (t.Input.Count > 0 || t.Output.Count > 0)
        {
            log.Append($"[{t.Input.Select(x => x.Id.ToString()).Join(",")}");
            if (t.Output.Count > 0)
                log.Append($"/{t.Output.Select(x => x.Id.ToString()).Join(",")}");
            log.Append($"]");
        }

        if (!string.IsNullOrEmpty(t.Accessor))
            log.Append($"'{t.Accessor}'");

    }

}