
using FlexibleParsingLanguage.Compiler;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Test;

[TestClass]
public class CustomFunctionTest
{
    class DateTimeParser : IConverterFunction
    {
        public string Name => "datetime";

        public object Convert(object value, object[] param)
        {
            if (value is not string raw)
                raw = value.ToString();

            return DateTime.Parse(raw).ToUniversalTime();
        }
    }


    class DoNothing : IConverterFunction
    {
        public string Name => "donothing";

        public object Convert(object value, object[] param)
        {
            return value;
        }
    }

    class DeJsoniserParser : IConverterFunction
    {
        public string Name => "dejson";

        public object Convert(object value, object[] param)
        {
            return System.Text.Json.JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });
        }
    }


    internal static IEnumerable<object> Flatten(IEnumerable x)
    {
        foreach (var item in x)
        {
            if (item is IEnumerable nested && item is not string)
            {
                foreach (var subItem in Flatten(nested))
                {
                    yield return subItem;
                }
            }
            else
            {
                yield return item;
            }
        }
    }

    class Concatenater : IConverterFunction
    {
        public string Name => "concat";

        public object Convert(object value, object[] param)
        {
            var cat = new StringBuilder();

            foreach (var item in Flatten(param))
            {
                if (item is not string str)
                    str = item?.ToString() ?? string.Empty;
                cat.Append(str);
            }
            return cat.ToString();
        }
    }

    class Joiner : IConverterFunction
    {
        public string Name => "join";

        public object Convert(object value, object[] param)
        {
            var joined = new StringBuilder(value.ToString());
            joined.Append("=");

            var ro = new List<string>();

            foreach (var item in param)
            {
                if (item is IEnumerable it)
                {
                    var r = new List<string>();
                    foreach (var subItem in it)
                    {
                        if (subItem is not string str)
                            str = subItem?.ToString() ?? string.Empty;
                        r.Add(str);
                    }
                    ro.Add("[" + string.Join(",", r) + "]");
                }
                else
                {
                    ro.Add(item.ToString());
                }
            }

            joined.Append(string.Join(";", ro));

            return joined.ToString();
        }
    }

    class AlphaFilter : IFilterFunction
    {
        public string Name => "alpha";
        public bool Filter(object value, object[] param)
        {
            if (value is not string str)
                str = value?.ToString() ?? string.Empty;

            var filter = Flatten(param).Select(x => x.ToString() ?? string.Empty).Concat();

            return string.CompareOrdinal(str, filter) <= 0;
        }
    }

    class IndexReducer : IConverterFunction
    {
        public string Name => "index_of";

        public object Convert(object value, object[] param)
        {
            if (value is not IEnumerable it)
                return -1;
            var p = param[0].ToString();

            var i = 0;
            foreach (var item in it)
            {
                if (item.ToString() == p)
                    return i;
                i++;
            }
            return -1;
        }
    }

    class ByIndexSelector : IConverterFunction
    {
        public string Name => "by_index";

        public object Convert(object value, object[] param)
        {
            var p = param[0];
            var t = p.GetType().Name;
            var i = int.Parse(p.ToString());

            if (value is IList list)
                return list[i];

            if (value is JsonArray arr)
                return arr[i];

            throw new Exception($"Cannot select index {i} from type {value.GetType().Name} with param type {t}");
        }
    }


    [TestMethod]
    public void RecursiveFunctionTest()
    {
        var payload = "{\"aaa\":\"bbb\"}";
        var query = $"|json|dejson|json.aaa";

        var compiler = new FplCompiler();
        compiler.RegisterConverter(new DeJsoniserParser());
        var parser = compiler.Compile(query);
        var result = parser.Parse(payload);
        Assert.AreEqual("bbb", ((List<object>)result)[0].ToString());
    }


    [TestMethod]
    public void DatetimePayloadTest()
    {
        var payload = "{\"data\":\"2024-01-15T20:11:17+01:00\"}";
        var query = $"|json.data|datetime";

        var compiler = new FplCompiler();
        compiler.RegisterConverter(new DateTimeParser());
        var parser = compiler.Compile(query);
        var result = parser.Parse(payload);
        Assert.AreEqual(DateTime.Parse("2024-01-15T19:11:17+00:00").ToUniversalTime(), ((List<object>)result)[0]);
    }

    [TestMethod]
    public void MultiParamTest()
    {
        var payload = "{\"ak\":\"av\", \"bk\":\"bv\"}";
        var query = $"|json|concat(@.ak,@.bk,'cv')";
        var compiler = new FplCompiler();
        compiler.RegisterConverter(new Concatenater());
        var parser = compiler.Compile(query);
        var result = parser.Parse(payload);
        Assert.AreEqual(1, ((List<object>)result).Count);
        Assert.AreEqual("avbvcv", ((List<object>)result)[0].ToString());
    }

    [TestMethod]
    public void MultiParamComplexTest()
    {
        var payload = "{\"r\": {}, \"data\": [{\"a\":\"a1\", \"b\":\"b1\"}, {\"a\":\"a2\", \"b\":\"b2\"}]}";
        var query = $"|json.data*|concat(@.a,@.b)";
        var compiler = new FplCompiler();
        compiler.RegisterConverter(new Concatenater());
        var parser = compiler.Compile(query);
        var result = parser.Parse(payload);
        Assert.AreEqual("[\"a1b1\",\"a2b2\"]", JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false }));
    }

    [TestMethod]
    public void MultiParamSaveTest()
    {
        var payload = "{\"data\": [{\"p1\":{\"p2\":\"a1\",\"p4\": \"a4\"}, \"p3\":\"a2\"}, {\"p1\":{\"p2\":\"b1\",\"p4\": \"b4\"}, \"p3\":\"b2\"}, {\"p1\":{\"p2\":\"c1\",\"p4\": \"c4\"}, \"p3\":\"c2\"}]}";
        var query = $"|json.data*@@save1.p1@@save2|concat(@.p2,@save1.p3,@save2.p4)";
        var compiler = new FplCompiler();
        compiler.RegisterConverter(new Concatenater());
        var parser = compiler.Compile(query);
        var result = parser.Parse(payload);
        Assert.AreEqual("[\"a1a2a4\",\"b1b2b4\",\"c1c2c4\"]", JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false }));
    }


    [TestMethod]
    public void MultiParamSaveTest2()
    {
        var payload = new Dictionary<string, object>
        {
            { "r", new List<object> { "r1", "r2" } },
            { "data", new List<object> {
                new Dictionary<string, object> { { "p1", new Dictionary<string, string> { { "p2", "a1" } } }, { "p3", "a2" } },
                new Dictionary<string, object> { { "p1", new Dictionary<string, string> { { "p2", "b1" } } }, { "p4", "b2" } },
                new Dictionary<string, object> { { "p1", new Dictionary<string, string> { { "p2", "c1" } } }, { "p4", "c2" } }
                }
            }
        };

        var query = $"data*p1@@save|concat($r,@.p2,@save.p3)";
        var compiler = new FplCompiler();
        compiler.RegisterConverter(new Concatenater());
        var parser = compiler.Compile(query);
        var result = parser.Parse(payload);
        Assert.AreEqual("[\"r1r2a1\",\"r1r2b1\",\"r1r2c1\"]", JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false }));
    }

    /*
    [TestMethod]
    public void EmptyParamTest()
    {
        var payload = "{\"v\": [\"a\", \"b\"], \"n\": 77}";
        var query = $"|json@@s.v*|join(@s.n|donothing())";
        var compiler = new FplCompiler();
        compiler.RegisterConverter(new Joiner());
        compiler.RegisterConverter(new DoNothing());
        var parser = compiler.Compile(query);
        var result = parser.Parse(payload);
        Assert.AreEqual("?", JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false }));
    }
    */

    [TestMethod]
    [DataRow(
        "FindIndex",
        "[{\"header\": [ \"a\", \"b\", \"v\", \"h\" ],\"values\": [ 54, 23, 101, 12, 9 ]},{\"header\": [ \"h\", \"v\", \"a\", \"b\" ],\"values\": [ 23, 102, 75, 12, 9 ]},{\"header\": [ \"h\", \"a\", \"b\", \"v\" ],\"values\": [ 23, 75, 12, 103 ]}]",
        $"|json*@@s.values|by_index(@s.header|index_of('v'))",
        "[101,102,103]"
    )]
    [DataRow(
        "JoinIndexOf",
        "{\"v\": [\"a\", \"b\"], \"h1\": [ \"a1\", \"b1\", \"c1\", \"d1\" ], \"h2\": [ \"a2\", \"b2\", \"c2\", \"d2\" ], \"h3\": [ \"a3\", \"b3\", \"c3\", \"d3\" ], \"n\": [1,2,3,4,5,6,7,8,9]}",
        "|json@@s.v*|join(@s.h1, @s.h2, @s.h3.2, @s.h2|index_of('d2'))",
        "[\"a=[a1,b1,c1,d1];[a2,b2,c2,d2];c3;3\",\"b=[a1,b1,c1,d1];[a2,b2,c2,d2];c3;3\"]"
    )]
    [DataRow(
        "Join",
        "{\"v\": [\"a\", \"b\"], \"n\": 77, \"n2\": [78], \"na\": {\"nb\":79}}",
        "|json@@s.v*|join(@s.n,@s.n2*,@s.n|donothing(''),@s.n2*|donothing(''),@s.na.nb)",
        "[\"a=77;[78];77;[78];79\",\"b=77;[78];77;[78];79\"]"
    )]
    [DataRow(
        "FilterParam",
        "[{\"f\":\"5\", \"d1\":{\"d2\":[1,3,4,5,6,7,8,9]}}]",
        "|json*@@s.d1.d2*|alpha(@s.f)",
        "[1,3,4,5]"
    )]
    [DataRow(
        "MultiParamMultiIterSave",
        @"[
            {'px1': [{'k':'va1'}, {'k':'va2'}], 'px2': 'xa'},
            {'px1': [{'k':'vb1'}, {'k':'vb2'}], 'px2': 'xb'},
            {'px1': [{'k':'vc1'}, {'k':'vc2'}], 'px2': 'xc'},
            {'px1': [{'k':'vd1'}, {'k':'vd2'}], 'px2': 'xd'}
        ]",
        "|json*@@s1.px1*|concat(@.k,@s1.px2)",
        "[\"va1xa\",\"va2xa\",\"vb1xb\",\"vb2xb\",\"vc1xc\",\"vc2xc\",\"vd1xd\",\"vd2xd\"]"
    )]
    public void FunctionTest(string name, string payload, string query, string expected)
    {
        payload = payload.Replace("'", "\"");
        var compiler = new FplCompiler();
        compiler.RegisterConverter(new Joiner());
        compiler.RegisterConverter(new IndexReducer());
        compiler.RegisterConverter(new ByIndexSelector());
        compiler.RegisterConverter(new DoNothing());
        compiler.RegisterFilter(new AlphaFilter());
        compiler.RegisterConverter(new Concatenater());
        var parser = compiler.Compile(query);
        var result = parser.Parse(payload);
        Assert.AreEqual(expected, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false }));
    }








    /*
    [TestMethod]
    public void FindIndex2Test()
    {
        var payload = "[{\"header\": [ \"a\", \"b\", \"v\", \"h\" ],\"values\": [ 54, 23, 101, 12, 9 ]},{\"header\": [ \"h\", \"v\", \"a\", \"b\" ],\"values\": [ 23, 102, 75, 12, 9 ]},{\"header\": [ \"h\", \"a\", \"b\", \"v\" ],\"values\": [ 23, 75, 12, 103 ]}]";
        var query = $"|json*@@s.values(@s.header|index_of('v'))";
        var compiler = new FplCompiler();
        compiler.RegisterReducer(new IndexReducer());
        var parser = compiler.Compile(query);
        var result = parser.Parse(payload);
        Assert.AreEqual("[101,102,103]", JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false }));
    }
    */
}
