using System.Text.Json;
using System.Text.Json.Nodes;

namespace FlexibleParsingLanguage.Test;

[TestClass]
public class ParserTest
{
    private Lexicalizer L { get; set; } = new Lexicalizer();

    [TestMethod]
    public void JsonParserTest() {

        var serializationOptions = new JsonSerializerOptions();
        serializationOptions.WriteIndented = true;

        foreach (var f in Directory.EnumerateFiles("../../../JsonPayloads").Where(f => !f.EndsWith(".result.json") && !f.EndsWith(".query"))) {

            var text = File.ReadAllText(f);
            var query = File.ReadAllText(f.Replace(".json", ".query"));
            var parser = L.Lexicalize(query);
            var raw = JsonSerializer.Deserialize<JsonNode>(text);

            var result = parser.Parse(raw);
            var serialized = JsonSerializer.Serialize(result, serializationOptions);
            var expected = File.ReadAllText(f.Replace(".json", ".result.json"));
            Assert.AreEqual(expected, serialized, $"parsing result {f}");
        }
    }
}