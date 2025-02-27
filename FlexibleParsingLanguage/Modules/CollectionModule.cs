﻿using FlexibleParsingLanguage.Parse;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Formats.Tar;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Modules;

internal class CollectionWritingModule : IWritingModule
{
    public object BlankArray() => new List<object>();

    public object BlankMap() => new Dictionary<string, object>();

    public void Write(object raw, string acc, object val)
    {
        if (raw is not IDictionary dict)
        {
#if DEBUG
            throw new Exception($"Tried to string write to {raw?.GetType().FullName ?? "null"} ");
#endif
            return;
        }

        dict.Add(acc, val);
    }

    public void Write(object raw, int acc, object val)
    {
        if (raw is not IList list)
            return;
        list[acc] = val;
    }

    public void Append(object target, object? val)
    {






        switch (target)
        {
            case IList list:
                list.Add(val);
                break;
            case IDictionary dict:
                dict.Add(Guid.NewGuid().ToString(), val);
                break;
            default:
#if DEBUG
                throw new Exception($"Tried to Append to {target?.GetType().FullName ?? "null"} ");
#endif
                break;
        }


    }
}
