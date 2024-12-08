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
using static FlexibleParsingLanguage.Parse.ParsingContext;

namespace FlexibleParsingLanguage.Test;

[TestClass]
public class Parsing
{
    private Lexicalizer L { get; } = new Lexicalizer();

    public static IEnumerable<object[]> SimpleQueries => new List<object[]>
    {
        new object[] { "ConvertXmlTest", "<a><b1>bbb</b1><b2>bb2</b2></a>", "|xml.a.b2:h1", "{'h1':'bb2'}", null, null },
        new object[] { "ConvertJsonTest", "{'a':{'b1':'bbb','b2':'bb2'}}", "|json.a.b2:h1", "{'h1':'bb2'}", null, null },
        new object[] { "ConvertConfigTest", "[{'id': 'bob'}, {'id': 'trj'}]", "|json*id@", "['n53','a81']", new List<(string, string)> { ("bob", "n53"), ("trj", "a81") }, new List<List<(string, string)>> { } },
    };


    [TestMethod]
    [DynamicData(nameof(SimpleQueries))]
    public void SimpleParserTest(string name, string payload, string query, string expected, List<(string, string)> config, List<List<(string, string)>> configEntries) => TestCompleteParsingStep(payload, query, expected, config, configEntries, true);

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
