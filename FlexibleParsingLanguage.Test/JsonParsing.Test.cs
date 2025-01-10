using FlexibleParsingLanguage.Compiler;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FlexibleParsingLanguage.Test;

[TestClass]
public class JsonParsingTest
{
    public static IEnumerable<object[]> PayloadFiles
    {
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
        
        new object[] { "Foreach Example 1", "[[[\"a\",\"b\"],[\"c\",\"d\"]],[[\"e\",\"f\"],[\"g\",\"h\"]]]", "***", "[\"a\",\"b\",\"c\",\"d\",\"e\",\"f\",\"g\",\"h\"]" },
        new object[] { "Foreach Example 2", "[[[\"a\",\"b\"],[\"c\",\"d\"]],[[\"e\",\"f\"],[\"g\",\"h\"]]]", "*:***", "[[\"a\",\"b\",\"c\",\"d\"],[\"e\",\"f\",\"g\",\"h\"]]" },
        new object[] { "Foreach Example 3", "[[[\"a\",\"b\"],[\"c\",\"d\"]],[[\"e\",\"f\"],[\"g\",\"h\"]]]", "**:**", "[[\"a\",\"b\"],[\"c\",\"d\"],[\"e\",\"f\"],[\"g\",\"h\"]]" },
        new object[] { "Foreach Example 4", "[[[\"a\",\"b\"],[\"c\",\"d\"]],[[\"e\",\"f\"],[\"g\",\"h\"]]]", "**:*:h*", "[{\"h\":[\"a\",\"b\"]},{\"h\":[\"c\",\"d\"]},{\"h\":[\"e\",\"f\"]},{\"h\":[\"g\",\"h\"]}]" },

        new object[] { "Branch Example", """[{"k1":1, "k2": 11}, {"k1":2, "k2": 12}, {"k1":3, "k2": 13}]""", "*:*{@k1:h1}k2:h2", """[{"h1":1,"h2":11},{"h1":2,"h2":12},{"h1":3,"h2":13}]""" },




        new object[] { "Single Query With Header", "{ \"k\": \"test_v\" }", "k:h", "{\"h\":\"test_v\"}" },
        new object[] { "Single Query", "{ \"k\": \"test_v\" }", "k", "[\"test_v\"]" },
        new object[] { "Key Only", "{ \"k\" : \"v\" }", "k", "[\"v\"]" },
        new object[] { "Key Header", "{ \"k\" : \"v\" }", "k:h", "{\"h\":\"v\"}" },
        new object[] { "Read depth", "{ \"a\": { \"a\": \"value\" }}", "a.a:bb", "{\"bb\":\"value\"}" },
        new object[] { "Write depth", "{ \"aa\": \"value\" }", "aa:b:b", "{\"b\":{\"b\":\"value\"}}" },

        new object[] { "", "{ \"root\": { \"k1\": \"v1\", \"k2\":\"v2\" }}", "root{@k2}k1", "[\"v2\",\"v1\"]" },
        new object[] { "", "{ \"root\": { \"k1\": \"v1\", \"k2\":\"v2\" }}", "root{@k1:h1}k2:h2", "{\"h1\":\"v1\",\"h2\":\"v2\"}" },
        new object[] { "", "{ \"root\": [{\"v\": 1}, {\"v\": 2}, {\"v\": 3}]}", "root*v", "[1,2,3]" },
        new object[] { "Foreach Array", "{ \"root\": [{\"v1\": {\"v2\": 1}}, {\"v1\": {\"v2\": 2}}, {\"v1\": {\"v2\": 3}}]}", "root*v1.v2", "[1,2,3]" },
        new object[] { "", "{ \"root\": [{\"v\": [1, 11, 111]}, {\"v\": [2, 22, 222]}, {\"v\": [3, 33, 333]}]}", "root*v*", "[1,11,111,2,22,222,3,33,333]" },
        new object[] { "", "{ \"root\": [{\"v\": [{\"v2\": 1}, {\"v2\": 11}, {\"v2\": 111}]}, {\"v\": [{\"v2\": 2}, {\"v2\": 22}, {\"v2\": 222}]}, {\"v\": [{\"v2\": 3}, {\"v2\": 33}, {\"v2\":333}]}]}", "root*v*v2", "[1,11,111,2,22,222,3,33,333]" },
        new object[] { "", "{ \"root\": [{\"v\": 1}, {\"v\": 2}, {\"v\": 3}]}", "root*v:h", "{\"h\":[1,2,3]}" },
        new object[] { "Escape check", "{\"w'k\": \"value\"}", "\"w'k\":header", "{\"header\":\"value\"}" },
    };

    [TestMethod]
    [DynamicData(nameof(JsonQueries))]
    public void JsonParserTest(string name, string payload, string query, string expected) => TestCompleteParsingStep(payload, query, expected, null);

    public static IEnumerable<object[]> SimpleJsonQueries => new List<object[]>
    {


        new object[] { "Simple Test", "{'k': 'v'}", "k", "['v']" },
        new object[] { "Simple Header Test", "{'k': 'v'}", "k:h", "{'h':'v'}" },
        new object[] { "Foreach Test", "{'a': 1, 'b': 2, 'c': 3}", "*:*:h", "[{'h':1},{'h':2},{'h':3}]" },
        new object[] { "Numeric Query", "['a', {'2':'b'}, 'c']", "1'2'", "['b']" },
        new object[] { "Foreach 1A", "[[['a11','a12'],['a21','a22']],[['b11','b12'],['b21','b22']]]", "*", "[[['a11','a12'],['a21','a22']],[['b11','b12'],['b21','b22']]]" },
        new object[] { "Foreach 1B", "[[['a11','a12'],['a21','a22']],[['b11','b12'],['b21','b22']]]", "**", "[['a11','a12'],['a21','a22'],['b11','b12'],['b21','b22']]" },
        new object[] { "Foreach 1C", "[[['a11','a12'],['a21','a22']],[['b11','b12'],['b21','b22']]]", "***", "['a11','a12','a21','a22','b11','b12','b21','b22']" },
        new object[] { "Foreach 2A", "{'a':[['a11','a12'],['a21','a22']],'b':[['b11','b12'],['b21','b22']]}", "*", "[[['a11','a12'],['a21','a22']],[['b11','b12'],['b21','b22']]]" },
        new object[] { "Foreach 2B", "{'a':[['a11','a12'],['a21','a22']],'b':[['b11','b12'],['b21','b22']]}", "**", "[['a11','a12'],['a21','a22'],['b11','b12'],['b21','b22']]" },
        new object[] { "Foreach 2C", "{'a':[['a11','a12'],['a21','a22']],'b':[['b11','b12'],['b21','b22']]}", "***", "['a11','a12','a21','a22','b11','b12','b21','b22']" },
        new object[] { "Interupted Foreach A", "[[['a11','a12'],['a21','a22']],[['b11','b12'],['b21','b22']]]", "*:***", "[['a11','a12','a21','a22'],['b11','b12','b21','b22']]" },
        new object[] { "Interupted Foreach B", "[[['a11','a12'],['a21','a22']],[['b11','b12'],['b21','b22']]]", "**:**", "[['a11','a12'],['a21','a22'],['b11','b12'],['b21','b22']]" },
        new object[] { "Interupted Foreach C", "[[['a11','a12'],['a21','a22']],[['b11','b12'],['b21','b22']]]", "***:*", "[['a11'],['a12'],['a21'],['a22'],['b11'],['b12'],['b21'],['b22']]" },
        new object[] { "Interupted Foreach 2C", "[[['a11','a12'],['a21','a22']],[['b11','b12'],['b21','b22']]]", "***:*:h", "[{'h':'a11'},{'h':'a12'},{'h':'a21'},{'h':'a22'},{'h':'b11'},{'h':'b12'},{'h':'b21'},{'h':'b22'}]" },


        new object[] { "Foreach Header", "[[1,2],[3]]", "**:*:v", "[{'v':1},{'v':2},{'v':3}]"},

        new object[] { "Group Accessor Test", "{'a': { 'b': { 't28': 'v' } }, 'metadata': {'idkey': 't28'}}", "a.b($metadata.idkey)", "['v']" },

        new object[] { "Root vs Param Test A", "[{'n': 'a', 'v':1},{'n': 'b', 'v':2}]", "*:*{$*n:nn}v:w", "[{'nn':['a','b'],'w':1},{'nn':['a','b'],'w':2}]"},
        new object[] { "Root vs Param Test B", "[{'n': 'a', 'v':1},{'n': 'b', 'v':2}]", "*:*{@n:nn}v:w", "[{'nn':'a','w':1},{'nn':'b','w':2}]"},

        new object[] { "Simple branch test", "{'k': {'ka': 'va', 'kb': 'vb'}}", "k{@ka:ha}kb:hb", "{'ha':'va','hb':'vb'}" },
        new object[] { "Unbranch test", "{'a': {'f':1, 'f2': 11}, 'b': {'f':2, 'f2': 12}, 'c': {'f':3, 'f2': 13}}", "*:*{@f:fh}f2:fh2", "[{'fh':1,'fh2':11},{'fh':2,'fh2':12},{'fh':3,'fh2':13}]" },

        new object[] { "Read Root Test", "{'name':'nv', 'values':[1,2,3]}", "values*:*{$name:n}:v", "[{'n':'nv','v':1},{'n':'nv','v':2},{'n':'nv','v':3}]"},
        new object[] { "Write Root Test", "{'k1': {'k2': 1, 'k3': 2}}", "k1:o1{@k2:o2}k3:$:o3", "{'o1':{'o2':1},'o3':2}"},

        new object[] { "Header Branching Test A", "[[1,2,3], [4, 5], [6, 8]]", "*:h1:h2", "{'h1':{'h2':[[1,2,3],[4,5],[6,8]]}}" },
        new object[] { "Header Branching Test C", "[[1,2,3], [4, 5], [6, 8]]", "*:h1:*:h2", "{'h1':[{'h2':[1,2,3]},{'h2':[4,5]},{'h2':[6,8]}]}" },
        new object[] { "Header Branching Test D", "[[1,2,3], [4, 5], [6, 8]]", "*.*:h1:*:h2", "{'h1':[{'h2':1},{'h2':2},{'h2':3},{'h2':4},{'h2':5},{'h2':6},{'h2':8}]}" },
        new object[] { "Header Branching Test E", "[[1,2,3], [4, 5], [6, 8]]", "*:*:a*:*:b", "[{'a':[{'b':1},{'b':2},{'b':3}]},{'a':[{'b':4},{'b':5}]},{'a':[{'b':6},{'b':8}]}]" },

        new object[] { "Read depth 3", "{ 'a': { 'a': { 'a': 'value' } }}", "a.a.a:bb", "{'bb':'value'}" },
        new object[] { "Write dept 3", "{ 'aa': 'value' }", "aa:b1:b2:b3", "{'b1':{'b2':{'b3':'value'}}}" },

        new object[] { "Name operator test", "{'a': 1, 'b': 2, 'c': 3}", "*:*{@~:n}:v", "[{'n':'a','v':1},{'n':'b','v':2},{'n':'c','v':3}]" },
        new object[] { "Multi Foreach", "[[[[1,2,3],[11,12,13]],[[21,22,23],[31,42,53]]],[[[99]]]]", "****", "[1,2,3,11,12,13,21,22,23,31,42,53,99]" },
        new object[] { "Interupted Multi Foreach", "[[[[1,2,3],[11,12,13]],[[21,22,23],[31,42,53]]],[[[99]]]]", "**:***", "[[1,2,3,11,12,13],[21,22,23,31,42,53],[99]]" },

    };

    [TestMethod]
    [DynamicData(nameof(SimpleJsonQueries))]
    public void SimpleJsonParserTest(string name, string payload, string query, string expected) => TestCompleteParsingStep(payload, query, expected, null, true);

    private void TestCompleteParsingStep(string payload, string query, string expected, JsonSerializerOptions? serilizationOptions = null, bool singleQuotes = false)
    {
        FplQuery parser;
        try
        {
            parser = FplQuery.Compile(query, null);
        }
        catch (QueryException ex)
        {
            ex.Query = query;
            Assert.Fail(ex.GenerateMessage());
            return;
        }

        try
        {
            if (singleQuotes)
                payload = payload.Replace('\'', '"');

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