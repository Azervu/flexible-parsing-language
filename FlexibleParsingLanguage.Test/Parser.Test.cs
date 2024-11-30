using System.Text.Json;
using System.Text.Json.Nodes;

namespace FlexibleParsingLanguage.Test;

[TestClass]
public class ParserTest
{
    private Lexicalizer L { get; } = new Lexicalizer();

    public static IEnumerable<object[]> PayloadFiles
    {
        /*
        *{k1:h1{*.k2:{*:h2}}}
        *k1*k2*:h1.h2
        *k1*:h1*h2{k2*}
        */
        get => Directory.EnumerateFiles("../../../Payloads").Where(f => !f.EndsWith(".result.json") && !f.EndsWith(".query")).Select(x => new object[] { x });
    }

    [TestMethod]
    [DynamicData(nameof(PayloadFiles))]
    public void JsonFileParserTest(string payloadFile) {
        var payload = File.ReadAllText(payloadFile);
        var query = File.ReadAllText(payloadFile.Replace(".json", ".query"));
        var expected = File.ReadAllText(payloadFile.Replace(".json", ".result.json"));
        TestCompleteParsingStep(payload, query, expected, new JsonSerializerOptions { WriteIndented = true });
    }

    public static IEnumerable<object[]> JsonQueries => new List<object[]>
    {
        new object[] { "{ \"k\": \"test_v\" }", "k", "[\"test_v\"]" },
        new object[] { "{ \"k\" : \"v\" }", "k", "[\"v\"]" },
        new object[] { "{ \"k\" : \"v\" }", "k:h", "{\"h\":\"v\"}" },
        new object[] { "{ \"a\": { \"a\": \"value\" }}", "a.a:bb", "{\"bb\":\"value\"}" },
        new object[] { "{ \"aa\": \"value\" }", "aa:b.b", "{\"b\":{\"b\":\"value\"}}" },
        new object[] { "{ \"root\": { \"k1\": \"v1\", \"k2\":\"v2\" }}", "root{k2}k1", "[\"v2\",\"v1\"]" },
        new object[] { "{ \"root\": { \"k1\": \"v1\", \"k2\":\"v2\" }}", "root{k1:h1}{k2:h2}", "{\"h1\":\"v1\",\"h2\":\"v2\"}" },
        new object[] { "{ \"root\": [{\"v\": 1}, {\"v\": 2}, {\"v\": 3}]}", "root*v", "[1,2,3]" },
        new object[] { "{ \"root\": [{\"v\": {\"v\": 1}}, {\"v\": {\"v\": 2}}, {\"v\": {\"v\": 3}}]}", "root*v.v", "[1,2,3]" },
        new object[] { "{ \"root\": [{\"v\": [1, 11, 111]}, {\"v\": [2, 22, 222]}, {\"v\": [3, 33, 333]}]}", "root*v*", "[1,11,111,2,22,222,3,33,333]" },
        new object[] { "{ \"root\": [{\"v\": [{\"v2\": 1}, {\"v2\": 11}, {\"v2\": 111}]}, {\"v\": [{\"v2\": 2}, {\"v2\": 22}, {\"v2\": 222}]}, {\"v\": [{\"v2\": 3}, {\"v2\": 33}, {\"v2\":333}]}]}", "root*v*v2", "[1,11,111,2,22,222,3,33,333]" },
        new object[] { "{ \"root\": [{\"v\": 1}, {\"v\": 2}, {\"v\": 3}]}", "root*v:h", "{\"h\":[1,2,3]}" },
        new object[] { "[[[[1,2,3],[11,12,13]],[[21,22,23],[31,42,53]]],  [[[99]]]    ]", "*.*.*.*", "[1,2,3,11,12,13,21,22,23,31,42,53,99]" }
    };

    [TestMethod]
    [DynamicData(nameof(JsonQueries))]
    public void JsonParserTest(string payload, string query, string expected) => TestCompleteParsingStep(payload, query, expected, null);


    public static IEnumerable<object[]> SimpleJsonQueries => new List<object[]>
    {
        new object[] { "Header Branching Test A", "[[1,2,3], [4, 5], [6, 8]]", "*:h1.h2", "{'h1':{'h2':[[1,2,3],[4,5],[6,8]]}}" },
        new object[] { "Header Branching Test B", "[[1,2,3], [4, 5], [6, 8]]", "*:h1{:h2}", "{'h1':{'h2':[[1,2,3],[4,5],[6,8]]}}" },
        new object[] { "Header Branching Test C", "[[1,2,3], [4, 5], [6, 8]]", "*:h1*h2", "{'h1':[{'h2':[1,2,3]},{'h2':[4,5]},{'h2':[6,8]}]}" },
        new object[] { "Header Branching Test D", "[[1,2,3], [4, 5], [6, 8]]", "*.*:h1*h2", "{'h1':[{'h2':1},{'h2':2},{'h2':3},{'h2':4},{'h2':5},{'h2':6},{'h2':8}]}" },
        new object[] { "Header Branching Test E", "[[1,2,3], [4, 5], [6, 8]]", "*:*a{*:*b}", "[{'a':[{'b':1},{'b':2},{'b':3}]},{'a':[{'b':4},{'b':5}]},{'a':[{'b':6},{'b':8}]}]" },




    };


    [TestMethod]
    [DynamicData(nameof(SimpleJsonQueries))]
    public void SimpleJsonParserTest(string name, string payload, string query, string expected) => TestCompleteParsingStep(payload, query, expected, null, true);



    private void TestCompleteParsingStep(string payload, string query, string expected, JsonSerializerOptions? serilizationOptions = null, bool singleQuotes = false)
    {
        Parser parser;
        try
        {
            parser = L.Lexicalize(query);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Lexicalizations step failed | query = {query} | error = {ex.Message}\n\n{ex.StackTrace}" );
            return;
        }

        try
        {
            var raw = JsonSerializer.Deserialize<JsonNode>(payload);
            var result = parser.Parse(raw);
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