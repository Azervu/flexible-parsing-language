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
        get => Directory.EnumerateFiles("../../../JsonPayloads").Where(f => !f.EndsWith(".result.json") && !f.EndsWith(".query")).Select(x => new object[] { x });
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
        new object[] { "{\"k\" : \"v\"}", "k", "v" }
    };

    [TestMethod]
    [DynamicData(nameof(SimplePayloads))]
    public void SimpleJsonParserTest(string payload, string query, string expected)
    {
        try
        {
            var parser = L.Lexicalize(query);
            var result = parser.Parse(JsonSerializer.Deserialize<JsonNode>(payload));
            var serialized = JsonSerializer.Serialize(result, O);
            Assert.AreEqual(expected, serialized, $"parsing result {payload}");
        }
        catch (Exception ex)
        {
            Assert.Fail(payload + " " + ex.Message + "\n" + ex.StackTrace);
        }
    }
}