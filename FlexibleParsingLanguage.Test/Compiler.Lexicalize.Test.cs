using FlexibleParsingLanguage.Compiler;

namespace FlexibleParsingLanguage.Test;

[TestClass]
public class TokenizerTest
{

    public static IEnumerable<object[]> ValidQueries => new List<object[]>
    {
        new object[] {"Simple", "a", "1.($,'a')  2{(1)"},
        new object[] {"Chain", "a.b#cc2.d", "1.($,'a')  2.(1,'b')  3#(2,'cc2')  4.(3,'d')  5{(4)"},
        new object[] {"Redundancies", "$b.#c{@.d}", "1.($,'b')  2#(1,'c')  3{(2)  4.(2,'d')  5{(4)"},
        new object[] {"Non redundant", "b#c{@d}", "1.($,'b')  2#(1,'c')  3{(2)  4.(2,'d')  5{(4)" },
        new object[] {"Escape", "a.b'ee\\'e'c.d", "1.($,'a')  2.(1,'b')  3.(2,'ee\\'e')  4.(3,'c')  5.(4,'d')  6{(5)"},
        new object[] {"Branch Simple", "a{@b}c", "1.($,'a')  2.(1,'b')  3{(2)  4.(1,'c')  5{(4)"},
        new object[] {"Branch Multi", "a{@b}{@c}d", "1.($,'a')  2.(1,'b')  3{(2)  4.(1,'c')  5{(4)  6.(1,'d')  7{(6)"},
        new object[] {"Branch Root", "a{$b}c", "1.($,'a')  2.($,'b')  3{(2)  4.(1,'c')  5{(4)"},
        new object[] {"Branch Header", "a{@b1:h2}b2:h1", "1.($,'a')  2.(1,'b1')  3:(2,'h2')  4{(3)  5.(1,'b2')  6:(5,'h1')  7{(6)"},
        new object[] {"Branch Complicated", "a{{@b}@c}d{@e}{@f{@g}}", "1.($,'a')  2.(1,'b')  3{(2)  4.(1,'c')  5{(4)  6.(1,'d')  7{(6)  8.(6,'e')  9{(8)  10.(6,'f')  11{(10)  12.(10,'g')  13{(12)"},

        new object[] {"Simple Parameter Group", "a(@b)", "1.($,'a')  2.(1,'b')  3.(1,2)  4{(3)"},
        new object[] {"Parameter Group", "a#(@b2.c2)b.1", "1.($,'a')  2.(1,'b2')  3.(2,'c2')  4#(1,3)  5.(4,'b')  6.(5,'1')  7{(6)"},
        new object[] {"Parameter Group root", "a($b)", "1.($,'a')  2.($,'b')  3.(1,2)  4{(3)" },

        new object[] {"Multi Parameter Group", "a(@b,@c,@d)", "1.($,'a')  2.(1,'b')  3.(1,'c')  4.(1,'d')  5.(1,2,3,4)  6{(5)"},
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
            parsed = FplQuery.Compiler.Lexicalize(parserString);
        }
        catch (QueryCompileException ex)
        {
            ex.Query = parserString;
            Assert.Fail(ex.GenerateMessage());
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
            var parsed = FplQuery.Compiler.Lexicalize(query);
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