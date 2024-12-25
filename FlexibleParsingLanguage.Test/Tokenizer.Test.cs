using FlexibleParsingLanguage.Compiler.Util;
using System.Text;

namespace FlexibleParsingLanguage.Test;

[TestClass]
public class TokenizerTest
{
    private Lexicalizer T { get; set; } = new Compiler.Compiler().Lexicalizer;

    public static IEnumerable<object[]> ValidQueries => new List<object[]>
    {
        new object[] {"Simple", "a.b#cc2.d", "1.($,'a')  2.(1,'b')  3#(2,'cc2')  4.(3,'d')"},
        new object[] {"Redundant separator ", "$b.#c", "1.($,'b')  2#(1,'c')"},
        new object[] {"Non redundant version", "b#c", "1.($,'b')  2#(1,'c')" },
        new object[] {"Escape", "a.b'ee\\'e'c.d", "1.($,'a')  2.(1,'b')  3.(2,'ee\\'e')  4.(3,'c')  5.(4,'d')"},

        new object[] {"Branch Simple", "a{@b1:h2}b2:h1", "1.($,'a')  2.(1,'b1')  3:(2,'h2')  4{(3)  5.(1,'b2')  6:(5,'h1')"},
        new object[] {"Branch Complicated", "a{{@b}@c}d{@e}{@f{@g}}", "1.($,'a')  2.(1,'b')  3{(2)  4.(1,'c')  5{(4)  6.(1,'d')  7.(6,'e')  8{(7)  9.(6,'f')  10{(9)  11.(9,'g')  12{(11)"},

        new object[] {"Parameter Group", "a#(@b2.c2)b.1", "1{  2.[1,'a']  5.[2,'b1']  7:[5,'h2']  4{[7]  10.[2,'b2']  12:[10,'h1']"},

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

        var idCounter = 1;
        var hiddenIdCounter = 1000;
        var idMap = new Dictionary<int, int>();
        foreach (var op in parsed)
        {
            if (op.IsSimple())
                op.Id = hiddenIdCounter--;
            else
                op.Id = idCounter++;
        }

        var result = parsed.RawQueryToString().Replace("\r", string.Empty);
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
            if (ex.CompilerIssue)
                throw;

            //only QueryCompileException should be thrown - otherwise it's a library issue
            return;
        }
        Assert.Fail($"Failed to catch issue in {query}");
    }

}