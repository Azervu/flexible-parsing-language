using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Parse;

internal static class ParsingFocusData_NewReadExtension
{

    internal static void ReadInnerMultiple(this ParsingFocusData data, Func<FocusEntry, FocusEntry> transform)
    {


        //NextRead(Reads[Active.ReadId].Select(transform).ToList());
    }

}
