using System.Text.Json;
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
        new object[] { "ConvertXmlTest", "<a><b1>bbb</b1><b2>bb2</b2></a>", "|xml.a.b2:h1", "{'h1':'bb2'}", null, null },
        new object[] { "ConvertJsonTest", "{'a':{'b1':'bbb','b2':'bb2'}}", "|json.a.b2:h1", "{'h1':'bb2'}", null, null },
        new object[] { "Simple Lookup Test", "[{'id': 'bob'}, {'id': 'trj'}]", "|json*@id", "['bob','trj']", new List<(string, string)> { ("bob", "n53"), ("trj", "a81") }, new List<List<(string, string)>> { } },

        new object[] {
            "Lookup Test", "[{'id': 'id_1'}, {'id': 'id_2'}]", "@#id|json*@id@#name", "['na','nb']",
            new List<(string, string)> {},
            new List<List<(string, string)>> {
                new List<(string, string)> { ("id", "id_1"), ("name", "na") },
                new List<(string, string)> { ("id", "id_2"), ("name", "nb") },
            }
        },

        new object[] { "LookupTestB", "[{'name': 'name_a'}, {'name': 'name_b'}]", "@#name_config|json*@name@#id_config", "['n53','a81']", new List<(string, string)> { }, new List<List<(string, string)>> { 
            new List<(string, string)> { ("name_config", "name_a"), ("id_config", "id_a") },
            new List<(string, string)> { ("name_config", "name_b"), ("id_config", "id_b") },
        }},

    };


    [TestMethod]
    [DynamicData(nameof(SimpleQueries))]
    public void SimpleParserTest(string name, string payload, string query, string expected, List<(string, string)> config, List<List<(string, string)>> configEntries)
        => TestCompleteParsingStep(payload, query, expected, config, configEntries, true);

    private void TestCompleteParsingStep(string payload, string query, string expected, List<(string, string)> configData, List<List<(string, string)>> configEntries, bool singleQuotes = false)
    {
        var rootConfig = new ParsingConfigContext();

        if (configData != null)
            rootConfig.AddEntries(configData);

        if (configEntries != null)
            foreach (var entry in configEntries)
                rootConfig.AddContext(entry);

        Parser parser;
        try
        {
            parser = L.Lexicalize(query, rootConfig);
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

            var serialized = JsonSerializer.Serialize(result);
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
