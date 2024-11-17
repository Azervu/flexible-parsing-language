using FlexibleParsingLanguage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Test;

[TestClass]
public class UtilTest
{
    [TestMethod]
    public void NameTest()
    {
        Assert.AreEqual("(int, string, int)", typeof((int, string, int)).GetHumanReadableName());
        Assert.AreEqual("(int?, string)[]", typeof((int?, string)[]).GetHumanReadableName());
        Assert.AreEqual("string", typeof(string).GetHumanReadableName());
        Assert.AreEqual("int?", typeof(int?).GetHumanReadableName());
        Assert.AreEqual("int", typeof(int).GetHumanReadableName());
        Assert.AreEqual("int[]", typeof(int[]).GetHumanReadableName());
        Assert.AreEqual("List<float>", typeof(List<float>).GetHumanReadableName());
        Assert.AreEqual("Dictionary<string, float>", typeof(Dictionary<string, float>).GetHumanReadableName());
    }
}