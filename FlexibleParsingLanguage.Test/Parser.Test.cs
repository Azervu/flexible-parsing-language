using System.Text.Json;
using System.Text.Json.Nodes;

namespace FlexibleParsingLanguage.Test;

[TestClass]
public class ParserTest
{
    private Lexicalizer L { get; } = new Lexicalizer();
    private JsonSerializerOptions O { get; } = new JsonSerializerOptions { WriteIndented = true };

    public static IEnumerable<object[]> Payloads
    {
        get =>
            Directory.EnumerateFiles("../../../JsonPayloads").Where(f => !f.EndsWith(".result.json") && !f.EndsWith(".query")).Select(x => new object[] { x });
    }

    [TestMethod]
    [DynamicData(nameof(Payloads))]
    public void JsonParserTest(string payload) {
        try
        {
            var text = File.ReadAllText(payload);
            var query = File.ReadAllText(payload.Replace(".json", ".query"));
            var parser = L.Lexicalize(query);
            var raw = JsonSerializer.Deserialize<JsonNode>(text);

            var result = parser.Parse(raw);
            var serialized = JsonSerializer.Serialize(result, O);
            var expected = File.ReadAllText(payload.Replace(".json", ".result.json"));
            Assert.AreEqual(expected, serialized, $"parsing result {payload}");
        }
        catch (Exception ex)
        {
            Assert.Fail(payload + " " + ex.Message + "\n" + ex.StackTrace);
        }
    }

    public static IEnumerable<object[]> SimplePayloads => new List<object[]>
    {
        new object[] { "{\"k\": \"test_v\"}", "k", "[\"test_v\"]" },
        new object[] { "{\"k\" : \"v\"}", "k", "[\"v\"]" },
        new object[] { "{\"k\" : \"v\"}", "k:h", "{\"h\":\"v\"}" },
        new object[] { "{ \"a\": { \"a\": \"value\" }}", "a.a:bb", "{\"bb\":\"value\"}" },
        new object[] { "{ \"aa\": \"value\" }", "aa:b.b", "{\"b\":{\"b\":\"value\"}}" },
        new object[] { "{ \"root\": { \"k1\": \"v1\", \"k2\":\"v2\" }}", "root{k2}k1", "[\"v2\",\"v1\"]" },
        new object[] { "{ \"root\": { \"k1\": \"v1\", \"k2\":\"v2\" }}", "root{k1:h1}{k2:h2}", "{\"h1\":\"v1\",\"h2\":\"v2\"}" },
        new object[] { "{ \"root\": [{\"v\": 1}, {\"v\": 2}, {\"v\": 3}]}", "root*v", "[[1,2,3]]" },
        new object[] { "{ \"root\": [{\"v\": {\"v\": 1}}, {\"v\": {\"v\": 2}}, {\"v\": {\"v\": 3}}]}", "root*v.v", "[[1,2,3]]" },
        new object[] { "{ \"root\": [{\"v\": [1, 11, 111]}, {\"v\": [2, 22, 222]}, {\"v\": [3, 33, 333]}]}", "root*v*", "[1,11,111,2,22,222,3,33,333]" },

        new object[] { "{ \"root\": [{\"v\": [{\"v2\": 1}, {\"v2\": 11}, {\"v2\": 111}]}, {\"v\": [{\"v2\": 2}, {\"v2\": 22}, {\"v2\": 222}]}, {\"v\": [{\"v2\": 3}, {\"v2\": 33}, {\"v2\":333}]}]}", "root*v*v2", "[1,11,111,2,22,222,3,33,333]" },
        new object[] { "{ \"root\": [{\"v\": 1}, {\"v\": 2}, {\"v\": 3}]}", "root*v:h", "{\"h\":[1,2,3]}" },
    };

    [TestMethod]
    [DynamicData(nameof(SimplePayloads))]
    public void SimpleJsonParserTest(string payload, string query, string expected)
    {
        try
        {
            var parser = L.Lexicalize(query);
            var result = parser.Parse(JsonSerializer.Deserialize<JsonNode>(payload));
            var serialized = JsonSerializer.Serialize(result);
            Assert.AreEqual(expected, serialized, $"payload {payload}");
        }
        catch (Exception ex)
        {
            Assert.Fail(payload + " " + ex.Message + "\n" + ex.StackTrace);
        }
    }
}