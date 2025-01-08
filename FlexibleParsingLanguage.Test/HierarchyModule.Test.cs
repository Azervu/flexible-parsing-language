using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Modules;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FlexibleParsingLanguage.Test;

[TestClass]
public class HierarchyModuleTest
{
    public static IEnumerable<object[]> SimpleJsonQueries => new List<object[]>
    {
        new object[] { "Simple Hierarchy Test", "{'k1': 'a1', 'v': [{'k2': 'a2', 'v': [{'k3': 'a3'}, {'k3': 'a3.2'}]}]}", "{@k1:a}v*:*{@k2:b}v*:*k3:c", "[['a1','a2','a3.2'],['a1','a2','a3']]" },
    };

    [TestMethod]
    [DynamicData(nameof(SimpleJsonQueries))]
    public void SimpleJsonParserTest(string name, string payload, string query, string expected) => TestCompleteParsingStep(payload, query, expected, null, true);

    private void TestCompleteParsingStep(string payload, string query, string expected, JsonSerializerOptions? serilizationOptions = null, bool singleQuotes = false)
    {
        FplQuery parser;
        try
        {
            parser = FplQuery.Compile(query, null, new HierarchyModule([("a", null), ("b", null), ("c", null)]));
        }
        catch (QueryException ex)
        {
            ex.Query = query;
            Assert.Fail(ex.GenerateMessage());
            return;
        }

        try
        {
            payload = payload.Replace('\'', '"');

            var raw = JsonSerializer.Deserialize<JsonNode>(payload);
            var result = ((HierarchyEntry)parser.Parse(raw)).ExtractEntries();

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