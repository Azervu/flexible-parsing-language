using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;
using FlexibleParsingLanguage.Parse;
using FlexibleParsingLanguage.Compiler;
using System.Collections;

namespace FlexibleParsingLanguage.Test;

[TestClass]
public class Parsing
{
    private Lexicalizer L { get; } = new Lexicalizer();

    public static IEnumerable<object[]> SimpleQueries => new List<object[]>
    {
        new object[] { "ConverterTest", "<a><b1>bbb</b1><b2>bb2</b2></a>", "|xml.a.b2:h1", "{'h1':'bb2'}"},
    };

    [TestMethod]
    [DynamicData(nameof(SimpleQueries))]
    public void SimpleParserTest(string name, string payload, string query, string expected) => TestCompleteParsingStep(payload, query, expected, null, true);

    private void TestCompleteParsingStep(string payload, string query, string expected, JsonSerializerOptions? serilizationOptions = null, bool singleQuotes = false)
    {
        Parser parser;
        try
        {
            parser = L.Lexicalize(query);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Lexicalizations step failed | query = {query} | error = {ex.Message}\n\n{ex.StackTrace}");
            return;
        }

        try
        {
            if (singleQuotes)
                payload = payload.Replace('\'', '"');

            var result = parser.Parse(payload);


            object v = null;
            if (result is IDictionary dict)
            {
                foreach (var key in dict.Values)
                    v = key;
            }


            var serialized = JsonSerializer.Serialize(result, serilizationOptions);

            if (singleQuotes)
                serialized = serialized.Replace('"', '\'');

            Assert.AreEqual(expected, serialized, $"payload {payload}");
        }
        catch (Exception ex)
        {
            Assert.Fail(payload + " " + ex.Message + "\n" + ex.StackTrace);
        }
    }
}
