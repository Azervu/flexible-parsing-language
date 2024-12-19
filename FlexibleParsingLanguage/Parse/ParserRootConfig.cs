using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Parse;

internal class ParserRootConfig
{
    internal WriteType RootType { get; set; }
}


internal enum WriteType
{
    None = 0,
    Object = 1,
    Array = 2,
}