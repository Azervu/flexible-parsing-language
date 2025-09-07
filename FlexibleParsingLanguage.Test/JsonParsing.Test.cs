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

        new object[] { "Branch Example", """[{"k1":1, "k2": 11}, {"k1":2, "k2": 12}, {"k1":3, "k2": 13}]""", "*:*{@.k1:h1}k2:h2", """[{"h1":1,"h2":11},{"h1":2,"h2":12},{"h1":3,"h2":13}]""" },

        new object[] { "Multi Read foreach", "[1,7,2,7,7,3,4]", "[0,2,5]*", "[1,2,3]" },

        new object[] { "Single Query With Header", "{ \"k\": \"test_v\" }", "k:h", "{\"h\":\"test_v\"}" },
        new object[] { "Single Query", "{ \"k\": \"test_v\" }", "k", "[\"test_v\"]" },
        new object[] { "Key Only", "{ \"k\" : \"v\" }", "k", "[\"v\"]" },
        new object[] { "Key Header", "{ \"k\" : \"v\" }", "k:h", "{\"h\":\"v\"}" },
        new object[] { "Read depth", "{ \"a\": { \"a\": \"value\" }}", "a.a:bb", "{\"bb\":\"value\"}" },
        new object[] { "Write depth", "{ \"aa\": \"value\" }", "aa:b:b", "{\"b\":{\"b\":\"value\"}}" },

        new object[] { "", "{ \"root\": { \"k1\": \"v1\", \"k2\":\"v2\" }}", "root{@.k2}k1", "[\"v2\",\"v1\"]" },
        new object[] { "", "{ \"root\": { \"k1\": \"v1\", \"k2\":\"v2\" }}", "root{@.k1:h1}k2:h2", "{\"h1\":\"v1\",\"h2\":\"v2\"}" },
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
        new object[] { "Simple Test Desugarized", "{'k': 'v'}", "k:", "['v']" },
        new object[] { "Literal Header Test", "{'k': 'v'}", "k:''", "{'':'v'}" },

        new object[] { "Simple Header Test", "{'k': 'v'}", "k:h", "{'h':'v'}" },

        new object[] { "Branch Add", "{'r': {'k1': 'v1', 'k2': 'v2'}}", "r{k1}k2", "['v1','v2']" },
        new object[] { "Branch Add Desugared", "{'r': {'k1': 'v1', 'k2': 'v2'}}", "r@@s.k1:@s.k2:", "['v1','v2']" },

        new object[] { "Multi Branch", "{'r': {'k1': {'k11': 'v11', 'k12': 'v12'}, 'k2': {'k21': 'v21', 'k22': 'v22'}}}", "r{k1{k11}k12}k2{k21}k22", "['v11','v12','v21','v22']" },
        new object[] { "Multi Branch Desugared", "{'r': {'k1': {'k11': 'v11', 'k12': 'v12'}, 'k2': {'k21': 'v21', 'k22': 'v22'}}}", "r@@1.k1@@2.k11:@2.k12:@1.k2@@3.k21:@3.k22:", "['v11','v12','v21','v22']" },
        new object[] { "Branch Query", "{'r': {'k1': 1, 'k2': 2}}", "r{k1:h1}k2:h2", "{'h1':1,'h2':2}" },
        new object[] { "Branch Query Desugared",   "{'r': {'k1': 1, 'k2': 2}}", "r@@s.k1:h1@s.k2:h2", "{'h1':1,'h2':2}" },

        new object[] { "Write Branch", "{'ka':'va', 'kb':'vb'}", "{ka:ha}kb:hb", "{'ha':'va','hb':'vb'}"},
        new object[] { "Write Branch Desugared", "{'ka':'va', 'kb':'vb'}", "@@s.ka:ha@s.kb:hb", "{'ha':'va','hb':'vb'}"},

        new object[] { "Simple Foreach", "['a','b','c']", "*", "['a','b','c']" },
        new object[] { "Simple Foreach + Write", "['a','b','c']", "*:*", "[['a'],['b'],['c']]" },

        new object[] { "Read Root Test", "{'name':'nv', 'values':[1,2,3]}", "values*:*{$name:n}:v", "[{'n':'nv','v':1},{'n':'nv','v':2},{'n':'nv','v':3}]"},

        new object[] { "Partial Multi Select", "{'k': 'v'}", "['k', 'unk']", "[['v',null]]" },
        new object[] { "Simple Array Result", "{'k': ['v']}", "k", "[['v']]" },

        new object[] { "Several Reads", "{'a': {'b': 'x'}}", "a.b", "['x']" },

        new object[] { "Array Foreach A", "['v']", "*", "['v']" }, //   new object[] { "Array Foreach A", "['v']", "*", "['v']" },
        new object[] { "Array Foreach B", "{'k': ['v']}", "*", "[['v']]" },

        new object[] { "Array Foreach C", "{'k': ['v']}", "k*", "['v']" }, //         new object[] { "Array Foreach C", "{'k': ['v']}", "k*", "['v']" },

        new object[] { "Simple Header Test", "{'k': 'v'}", "k:h", "{'h':'v'}" },

        new object[] { "Foreach Array Value Test", "['a','b','c']", "*", "['a','b','c']" },
        new object[] { "Foreach Array Name Test", "['a','b','c']", "*~", "[0,1,2]" },

        new object[] { "Foreach Object Value Test", "{'a': 1, 'b': 2, 'c': 3}", "*", "[1,2,3]" },
        new object[] { "Foreach Object Name Test", "{'a': 1, 'b': 2, 'c': 3}", "*~", "['a','b','c']" },

        new object[] { "Foreach Write Test", "{'a': 1, 'b': 2, 'c': 3}", "*:h", "{'h':[1,2,3]}" },
        new object[] { "Foreach Write B Test", "{'a': 1, 'b': 2, 'c': 3}", "*:*:h", "[{'h':1},{'h':2},{'h':3}]" },

        new object[] { "MultiArrayTest A", "[['a1', 'a2'],['b1','b2']]", "*", "[['a1','a2'],['b1','b2']]" },
        new object[] { "MultiArrayTest B", "[['a1', 'a2'],['b1','b2']]", "**", "['a1','a2','b1','b2']" },

        new object[] { "Filter single value result", "{'k': ['a','b']}", "k*|regex('b')", "['b']" },

        new object[] { "Branch foreach alternate", "{'k1': 'a1', 'v': [{'k2': 'a2', 'v': [{'k3': 'a3'}, {'k3': 'a3.2'}]}]}", "{@.k1}v*{k2}v*k3", "['a1','a2','a3','a3.2']" },
        new object[] { "Branch then foreach write", "{'k1': 'a1', 'v': [{'k2': 'a2', 'v': [{'k3': 'a3'}, {'k3': 'a3.2'}]}]}", "{k1}v*:*{k2}v*k3", "['a1',['a2','a3','a3.2']]" },
        new object[] { "Branch write header", "{'k1': 'a1', 'v': [{'k2': 'a2', 'v': [{'k3': 'a3'}, {'k3': 'a3.2'}]}]}", "{k1:h1}v*{k2:h2}v*k3:h3", "{'h1':'a1','h2':['a2'],'h3':['a3','a3.2']}" },

        new object[] { "Repeated Branches", "{'root':{'a':'va','b':'vb','c':'vc'}}", "root{@.a:ha}{@.b:hb}c:hc", "{'ha':'va','hb':'vb','hc':'vc'}" },

        new object[] { "Deep Branch", "{'root':{'a1':{'a2':{'a3':'va'}},'b1':{'b2':{'b3':'vb'}},'c1':{'c2':{'c3':'vc'}}}}", "root{@.a1.a2.a3:a}{@.b1.b2.b3:b}c1.c2.c3:c", "{'a':'va','b':'vb','c':'vc'}" },

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

        new object[] { "Test A", "{'a': [1,2,3,4,5,6,7]}", "a*:h", "{'h':[1,2,3,4,5,6,7]}" },
        new object[] { "Test B", "{'a': [1,2,3,4,5,6,7]}", "a:h", "{'h':[1,2,3,4,5,6,7]}" },

        new object[] { "Single Read A", "{'a': 1, 'b': 2, 'c': 3}", "a", "[1]" },
        new object[] { "Multi Read A", "{'a': 1, 'b': 2, 'c': 3}", "['a', 'c']", "[[1,3]]" },
        new object[] { "Multi Read A Write", "{'a': 1, 'b': 2, 'c': 3}", "['a', 'c']:h", "{'h':[1,3]}" },
        new object[] { "Multi Read A Write Compare", "[1,3]", ":h", "{'h':[1,3]}" },
        new object[] { "Multi Read A Write Foreach", "{'a': 1, 'b': 2, 'c': 3}", "['a', 'c']*:*:h", "[{'h':1},{'h':3}]" },
        new object[] { "Multi Read A Write Foreach Compare ", "[1,3]", "*:*:h", "[{'h':1},{'h':3}]" },


        new object[] { "Multi Read B", "{'a': [1, 2, 3], 'b': [4, 5, 6], 'c': [7, 8, 9]}", "['a', 'c']", "[[[1,2,3],[7,8,9]]]" },
        new object[] { "Multi Read B Foreach 1", "{'a': [1, 2, 3], 'b': [4, 5, 6], 'c': [7, 8, 9]}", "['a', 'c']*", "[[1,2,3],[7,8,9]]" },
        new object[] { "Multi Read B Foreach", "{'a': [1, 2, 3], 'b': [4, 5, 6], 'c': [7, 8, 9]}", "['a', 'c']**", "[1,2,3,7,8,9]" },
        new object[] { "Multi Read B Foreach Compare ", "[[1,2,3],[7,8,9]]", "**", "[1,2,3,7,8,9]" },

        new object[] { "Multi Read B Write", "{'a': [1, 2, 3], 'b': [4, 5, 6], 'c': [7, 8, 9]}", "['a', 'c']:h", "{'h':[[1,2,3],[7,8,9]]}" },
        new object[] { "Multi Read B Write Compare ", "[[1,2,3],[7,8,9]]", ":h", "{'h':[[1,2,3],[7,8,9]]}" },

        new object[] { "Multi Read B Write Foreach", "{'a': [1, 2, 3], 'b': [4, 5, 6], 'c': [7, 8, 9]}", "['a', 'c']*:*:h", "[{'h':[1,2,3]},{'h':[7,8,9]}]" },
        new object[] { "Multi Read B Write Foreach Compare ", "[[1,2,3],[7,8,9]]", "*:*:h", "[{'h':[1,2,3]},{'h':[7,8,9]}]" },


        new object[] { "Foreach Header", "[[1,2],[3]]", "**:*:v", "[{'v':1},{'v':2},{'v':3}]"},
        new object[] { "Group Accessor Test", "{'a': { 'b': { 't28': 'v' } }, 'metadata': {'idkey': 't28'}}", "a.b($metadata.idkey)", "['v']" },
        new object[] { "Multi Group Accessor Test", "{'a': { 'b': { 't28': 'v', 'x31': 'v2' } }, 'metadata': {'idkey': 't28'}}", "a.b($metadata.idkey, 'x31')", "[['v','v2']]" },

        new object[] { "Root vs Param Test A", "[{'n': 'a', 'v':1},{'n': 'b', 'v':2}]", "*:*{$*n:nn}v:w", "[{'nn':['a','b'],'w':1},{'nn':['a','b'],'w':2}]"},
        new object[] { "Root vs Param Test B", "[{'n': 'a', 'v':1},{'n': 'b', 'v':2}]", "*:*{@.n:nn}v:w", "[{'nn':'a','w':1},{'nn':'b','w':2}]"},

        new object[] { "Simple branch test", "{'k': {'ka': 'va', 'kb': 'vb'}}", "k{@.ka:ha}kb:hb", "{'ha':'va','hb':'vb'}" },
        new object[] { "Unbranch test", "{'a': {'f':1, 'f2': 11}, 'b': {'f':2, 'f2': 12}, 'c': {'f':3, 'f2': 13}}", "*:*{f:fh}f2:fh2", "[{'fh':1,'fh2':11},{'fh':2,'fh2':12},{'fh':3,'fh2':13}]" },

        new object[] { "Read Root Test", "{'name':'nv', 'values':[1,2,3]}", "values*:*{$name:n}:v", "[{'n':'nv','v':1},{'n':'nv','v':2},{'n':'nv','v':3}]"},

        new object[] { "Header Branching Test A", "[[1,2,3], [4, 5], [6, 8]]", "*:h1:h2", "{'h1':{'h2':[[1,2,3],[4,5],[6,8]]}}" },
        new object[] { "Header Branching Test C", "[[1,2,3], [4, 5], [6, 8]]", "*:h1:*:h2", "{'h1':[{'h2':[1,2,3]},{'h2':[4,5]},{'h2':[6,8]}]}" },
        new object[] { "Header Branching Test D", "[[1,2,3], [4, 5], [6, 8]]", "*.*:h1:*:h2", "{'h1':[{'h2':1},{'h2':2},{'h2':3},{'h2':4},{'h2':5},{'h2':6},{'h2':8}]}" },
        new object[] { "Header Branching Test E", "[[1,2,3], [4, 5], [6, 8]]", "*:*:a*:*:b", "[{'a':[{'b':1},{'b':2},{'b':3}]},{'a':[{'b':4},{'b':5}]},{'a':[{'b':6},{'b':8}]}]" },

        new object[] { "Read depth 3", "{ 'a': { 'a': { 'a': 'value' } }}", "a.a.a:bb", "{'bb':'value'}" },
        new object[] { "Write dept 3", "{ 'aa': 'value' }", "aa:b1:b2:b3", "{'b1':{'b2':{'b3':'value'}}}" },

        new object[] { "Name operator test", "{'a': 1, 'b': 2, 'c': 3}", "*:*{@~:n}:v", "[{'n':'a','v':1},{'n':'b','v':2},{'n':'c','v':3}]" },
        new object[] { "Multi Foreach", "[[[[1,2,3],[11,12,13]],[[21,22,23],[31,42,53]]],[[[99]]]]", "****", "[1,2,3,11,12,13,21,22,23,31,42,53,99]" },
        new object[] { "Interupted Multi Foreach", "[[[[1,2,3],[11,12,13]],[[21,22,23],[31,42,53]]],[[[99]]]]", "**:***", "[[1,2,3,11,12,13],[21,22,23,31,42,53],[99]]" },

        new object[] { "Array Indexing Test", "[{'v': [-1, 7, 6944]}, {'v': [-2, 8, 5276]}, {'v': [-5, 9, 69076]}]", "*.v.1", "[7,8,9]" },


        new object[] { "Multi Save Test A", "[{'px1': [{'k':'va1'}, {'k':'va2'}], 'px2': 'xa'}]", "*@@s1.px1*{k}@s1.px2", "['va1','va2','xa']" },
        new object[] { "Multi Save Test B", "[{'px1': [{'k':'va1'}, {'k':'va2'}], 'px2': 'xa'}]", "*@@s1.px1*{@.k}@s1.px2", "['va1','va2','xa']" },

        //TODO consider fix
        //new object[] { "Multi Save Test C", "[{'px1': [{'k':'va1'}, {'k':'va2'}], 'px2': 'xa'}]", "*@@s1.px1*@@1.k:@1@s1.px2:", "['va1','va2','xa']" },
        //new object[] { "Multi Save Test E", "[{'px1': [{'k':'va1'}, {'k':'va2'}], 'px2': 'xa'}]", "*@@s1.px1*(@.k,@s1.px2)", "['va1','va2','xa']" },

        new object[] { "Multi Save Test D", "[{'px1': [{'k':'va1'}, {'k':'va2'}], 'px2': 'xa'}]", "*@@s1.px1*{k}{@s1.px2}", "['va1','va2','xa',{'k':'va1'},{'k':'va2'}]" }, //TODO dont add : 


    };

    [TestMethod]
    [DynamicData(nameof(SimpleJsonQueries))]
    public void SimpleJsonParserTest(string name, string payload, string query, string expected) => TestCompleteParsingStep(payload, query, expected, null, true);


    [TestMethod]
    [DynamicData(nameof(SimpleJsonQueries))]
    public void DesugaredJsonParserTest(string name, string payload, string query, string expected) => TestCompleteParsingStep(payload, query, expected, null, true, true);


    private void TestCompleteParsingStep(string payload, string query, string expected, JsonSerializerOptions? serilizationOptions = null, bool singleQuotes = false, bool desugarize = false)
    {
        FplQuery parser;
        try
        {
            parser = FplQuery.Compile(query, null);

            if (desugarize)
            {
                var firstDesugared = parser.DesugaredQuery;
                parser = FplQuery.Compile(firstDesugared, null);

                Assert.AreEqual(firstDesugared, parser.DesugaredQuery, $"First and second desugared\n    {firstDesugared}\n    {parser.DesugaredQuery}");
            }
        }
        catch (QueryException ex)
        {
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

            Assert.AreEqual(expected, serialized, $"\n   expected = {expected}\n     actual = {serialized}\n    payload = {payload}\n      query = {parser.DesugaredQuery}\n   rawQuery = {parser.RawQuery}");

        }
        catch (QueryException ex)
        {
            Assert.Fail(ex.GenerateMessage());
            return;
        }

    }







}