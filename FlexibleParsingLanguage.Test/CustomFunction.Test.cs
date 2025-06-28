
using FlexibleParsingLanguage.Compiler;
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

    class DeJsoniserParser : IConverterFunction
    {
        public string Name => "dejson";

        public object Convert(object value, object[] param)
        {
            return System.Text.Json.JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });
        }
    }


    class Concatenater : IConverterFunction
    {
        public string Name => "concat";

        public object Convert(object value, object[] param)
        {
            return value.ToString() + param.Select(x => x.ToString()).Concat();
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

    /*
    [TestMethod]
    public void MultiParamTest()
    {
        var payload = "{\"ak\":\"ab\", \"bk\":\"bv\"}";
        var query = $"|json|concat(ak,bk)";
        var compiler = new FplCompiler();
        compiler.RegisterConverter(new Concatenater());
        var parser = compiler.Compile(query);
        var result = parser.Parse(payload);
        Assert.AreEqual("avbv", ((List<object>)result)[0].ToString());
    }
    */

}
