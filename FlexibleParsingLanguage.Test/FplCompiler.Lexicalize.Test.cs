using FlexibleParsingLanguage.Compiler;

namespace FlexibleParsingLanguage.Test;

[TestClass]
public class FplCompilerLexicalizerTest
{
    public static IEnumerable<object[]> InvalidQueries => new List<object[]>
    {
        new object[] { "Un-ended escape", "a.b'sdf.c" },
        //new object[] { "Branching group ends with an infix operator", "a.b{c.d#}e" },
        //new object[] { "Invalid Write Target", "k1.k2:h$k3" },
        //new object[] { "Invalid parameter group", "v(index_of('v'))" },
    };

    [TestMethod]
    [DynamicData(nameof(InvalidQueries))]
    public void CatchInvalidQueryTest(string name, string query)
    {
        try
        {
            var parsed = FplQuery.Compiler.Lexicalize(query);
        }
        catch (QueryException ex)
        {
            if (ex.CompilerIssue)
                throw;

            //only QueryCompileException should be thrown - otherwise it's a library issue
            return;
        }
        Assert.Fail($"Failed to catch issue in {query}");
    }
}