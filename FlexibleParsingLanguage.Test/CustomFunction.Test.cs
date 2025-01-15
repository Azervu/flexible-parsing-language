
using FlexibleParsingLanguage.Compiler;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Test;

[TestClass]
public class CustomFunctionTest
{
    class DateTimeParser : IConverterFunction
    {
        public string Name => "datetime";

        public object Convert(object value)
        {
            if (value is not string raw)
                raw = value.ToString();

            return DateTime.Parse(raw).ToUniversalTime();
        }
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
        Assert.AreEqual(((List<object>)result)[0], DateTime.Parse("2024-01-15T19:11:17+00:00").ToUniversalTime());
    }
}
