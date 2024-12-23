using FlexibleParsingLanguage.Compiler.Util;
using System.Text;

namespace FlexibleParsingLanguage.Test;

[TestClass]
public class TokenizerTest
{
    private Lexicalizer T { get; set; } = new Compiler.Compiler().Lexicalizer;

    public static IEnumerable<object[]> ValidQueries => new List<object[]>
    {
        new object[] {"Simple", "a.b#cc2.d", "1{  2.[1,'a']  4.[2,'b']  6#[4,'cc2']  8.[6,'d']"},
        new object[] {"Redundant separator", "b.#c",  "1{  2.[1,'b']  4#[2,'c']"},
        new object[] {"Non redundant version", "b#c",  "1{  2.[1,'b']  4#[2,'c']"},
        new object[] {"Escape", "a.b'ee\\'e'c.d", "1{  2.[1,'a']  4.[2,'b']  6.[4,'ee\\'e']  8.[6,'c']  10.[8,'d']"},

        new object[] {"Branch", "a{b1:h2}b2:h1", "1{  2.[1,'a']  4{[2]  5.[2,'b1']  7:[5,'h2']  9}  10.[2,'b2']  12:[10,'h1']"},
    };

    public static IEnumerable<object[]> InvalidQueries => new List<object[]>
    {
        new object[] { "Un-ended escape", "a.b'sdf.c" },
        new object[] { "Branching group ends with an infix operator", "a.b{c.d#}e" },
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

        var diff = -1;
        var min = Math.Min(result.Count(), expected.Count());
        for (int i = 0; i < min; i++)
        {
            if (result[i] == expected[i])
                continue;
            diff = i;
            break;
        }

        if (diff == -1 && result.Count() != expected.Count())
            diff = min;

        if (diff != -1)
        {
            Assert.Fail($"Query mismatch {parserString}\n{new string(' ', diff)}↓\n{expected}\n{result}");
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
        if (proccessed.Contains(t.Id) || (t.Output.Count == 1 && t.Type.Category.Has(OpCategory.Accessor)))
            return;

        var input = t.GetInput().ToList();
        foreach (var inp in t.GetInput())
            LogEntry(proccessed, log, inp);

        if (log.Length > 0)
            log.Append("  ");

        proccessed.Add(t.Id);
        log.Append($"{t.Id}");
        log.Append((!string.IsNullOrEmpty(t.Type.Operator) ? t.Type.Operator : $"'{t.Accessor}'"));

        if (input.Count > 0)
        {
            log.Append($"[{input.Select(x => {
                if (x.Output.Count == 1 && x.Type.Category.Has(OpCategory.Accessor))
                    return (!string.IsNullOrEmpty(x.Type.Operator) ? x.Type.Operator : $"'{x.Accessor.Replace("'", "\\'")}'");
                return x.Id.ToString();
            }).Join(",")}");
            log.Append($"]");
        }
    }
}