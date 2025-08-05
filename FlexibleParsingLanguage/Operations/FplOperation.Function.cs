using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FlexibleParsingLanguage.Operations;

internal partial class FplOperation
{

    internal static readonly OpConfig Function = new OpConfig("|", OpSequenceType.LeftInput | OpSequenceType.RightInput | OpSequenceType.OptionalExtraInput, (p, o) => CompileFunction(p, o))
    {
        CompileType = OpCompileType.ReadArray,
    };

    private static IEnumerable<ParseOperation> CompileFunction(ParseData parser, RawOp op)
    {
        if (op.Input.Count < 2 || string.IsNullOrWhiteSpace(op.Input[1].Accessor))
            throw new QueryException(op, $"function withouth name");

        var acc = op.Input[1].Accessor;
        if (parser.Filters.TryGetValue(acc, out var f))
            return CompileFilterFunction(parser, op, f);

        if (parser.Converter.TryGetValue(acc, out var converter))
        {
            if (op.Input.Count == 2)
                return CompileSaveUtil(parser, op, 2, [new ParseOperation(op, (q, c, d) => OperationFunctionConvert2(q, c, d, converter))]);
            return CompileTransformerFunction(parser, op, converter);
        }

        throw new QueryException(op, $"unknown function '{acc}'");
    }

    private static IEnumerable<ParseOperation> CompileFilterFunction(ParseData parser, RawOp op, IFilterFunction func)
    {
        if (op.Input.Count < 2 || op.Input[1].Accessor == null)
            throw new QueryException(op, $"filter missing input");

        var parameters = CompiledParameter.CompileParameters(parser, op);

        return CompileSaveUtil(parser, op, -1, [new ParseOperation(op, (q, c, d) => OperationFunctionFilter(q, c, d, parameters, func))]);
    }

    internal static IEnumerable<ParseOperation> CompileTransformerFunction(ParseData parser, RawOp op, IConverterFunction converter)
    {
        var id = op.GetStatusId(parser);

        foreach (var x in FplOperation.EnsureLoaded(parser, op))
            yield return x;

        var parameters = CompiledParameter.CompileParameters(parser, op);

        yield return new ParseOperation(op, (q, c, d) => OperationFunctionConvertMultiParam(q, c, d, parameters, converter));

        parser.LoadedId[0] = id;

        foreach (var x in FplOperation.EnsureSaved(parser, op))
            yield return x;

        var sequences = new List<int>();

        //for (var i = 0; i < parameters)
        //return CompileSaveUtil(parser, op, 2, [new ParseOperation((q, c, i, a) => OperationFunctionConvert(q, c, i, a, converter))]);
        //CompileTransformerFunction(parser, op, 2, [new ParseOperation((q, c, i, a) => OperationFunctionConvert(q, c, i, a, converter))]);
        //CompileTransformerFunction(parser, op, 2, [new ParseOperation((q, c, i, a) => OperationFunctionConvert(q, c, i, a, converter))]);
        //return CompileSaveUtil(parser, op, 2, [new ParseOperation((q, c, i, a) => OperationFunctionConvert(q, c, i, a, converter))]);
    }



    internal static void OperationFunctionFilter(FplQuery query, ParsingContext context, ParseOperationData d, List<CompiledParameter> parameters, IFilterFunction filter)
    {
        context.Focus.ReadMultiParamForeach(d.Id, parameters, (w, p) =>
        {
            //context.UpdateReadModule(v);
            context.UpdateReadModule(w.Value);
            object raw;
            if (context.ReadingModule != null)
                raw = context.ReadingModule.ExtractValue(w.Value.V);
            else
                raw = w.Value.V;

            if (filter.Filter(raw, p))
                return [new KeyValuePair<object, object>(w.Key.V, w.Value.V)];

            return [];
        });



        /*

  context.Focus.ReadForeach(d.Id, (w) =>
  {
      context.UpdateReadModule(w.Value);
      object raw;
      if (context.ReadingModule != null)
          raw = context.ReadingModule.ExtractValue(w.Value.V);
      else
          raw = w.Value.V;

      if (filter.Filter(raw, [d.StringAcc]))
          return [new KeyValuePair<object, object>(w.Key.V, w.Value.V)];

      return [];
  });

  if (parser._filters.TryGetValue(acc, out var f))
  {
      context.Focus.ReadForeach((w) =>
      {
          context.UpdateReadModule(w);
          object raw;
          if (context.ReadingModule != null)
              raw = context.ReadingModule.ExtractValue(w);
          else
              raw = w.V;

          if (f.Filter())

          if (f.Convert(raw, out var result))
              return new List<KeyValuePair<object, object>> {
              new KeyValuePair<object, object>(w.V, result)
          };

          return new List<KeyValuePair<object, object>>();
      });
  }
  */

    }

    /*
    internal static void OperationFunctionConvert(FplQuery parser, ParsingContext context, ParseOperationData d, IConverterFunction converter)
    {
        context.ReadTransformValue(d.Id, (w) =>
	}
    */





    internal static void OperationFunctionConvertMultiParam(FplQuery parser, ParsingContext context, ParseOperationData data, List<CompiledParameter> parameters, IConverterFunction converter)
    {
        /*
        var secondarySequences = context.Focus.GenerateSequencesIntersectionReadConfig()
    .SelectMany(x => x.AVal.Foci)
    .Where(x => x.Config.Entries.ContainsKey(acc))
    .Select(x => x.Config.Entries[acc].Value)
    .ToList();
        */


        /*


        var read = context.Focus.Reads[focus.ReadId];
        var intersections = context.Focus.GenerateSequencesIntersection(
            read, read.Select(x => x.SequenceId).ToList(),
            config, config.Select(x => x.SequenceId).ToList()
        );

        */

#if DEBUG
        context.Focus.ValidateTree();
#endif

        var primaryFocus = context.Focus.Reads[context.Focus.Active.ReadId];

        var primarySequences = primaryFocus
            .Select(x => x.SequenceId)
            .ToList();

        var secondarySequences = new List<List<int>>();
        var secondaryFocuses = new List<(int, List<FocusEntry>)>();

        var outParameters = new List<object>(parameters.Count());
        for (var i = 0; i < parameters.Count; i++)
        {
            var p = parameters[i];
            if (p.IsLiteral)
            {
                outParameters.Add(p.Accessor);
                continue;
            }
            outParameters.Add(null);

            var node = context.Focus.Store[p.Id];
            var secFocus = context.Focus.Reads[node.ReadId];
            secondarySequences.Add(secFocus.Select(x => x.SequenceId).ToList());
            secondaryFocuses.Add((i, secFocus));
        }

        var sequenceIntersection = context.Focus.GenerateSequencesIntersectionInner(primarySequences, secondarySequences.ToArray());

#if DEBUG
        var log = new StringBuilder();
        log.AppendLine("> [" + primaryFocus.Select(x => x.SequenceId.ToString()).Join(", ") + "]");
        foreach (var s in secondarySequences)
            log.AppendLine($": [{s.Select(y => y.ToString()).Join(", ")}]");

        log.AppendLine();

        foreach (var (sequenceId, sequenceParameters) in sequenceIntersection)
        {
            log.AppendLine(sequenceId.ToString() + " ?");
            foreach (var p in sequenceParameters)
                log.AppendLine("[" + p.Foci.Select(x => x.SequenceId.ToString()).Join(", ") + "]");
        }

        log.AppendLine();

        foreach (var (id, fc) in secondaryFocuses)
        {
            log.AppendLine(id + " >> " + fc.Select(x => x.SequenceId + x.Value.V?.ToString()).Join(", "));
        }

        var debug = log.ToString();



        var l = new StringBuilder();



        foreach (var (sequenceId, sequenceParameters) in sequenceIntersection)
        {
            l.AppendLine(sequenceId.ToString());
            foreach (var p in sequenceParameters)
                l.AppendLine("    [" + p.Foci.Select(x => x.SequenceId.ToString()).Join(", ") + "]");
        }

        var ll = l.ToString();
#endif

        var result = new List<FocusEntry>(sequenceIntersection.Count());
        for (var i = 0; i < sequenceIntersection.Count(); i++)
        {
            var (sequenceId, sequenceParameters) = sequenceIntersection[i];

            if (sequenceId == -1)
                throw new Exception("SequenceId parent = -1");

            var primaryData = primaryFocus[i].Value;

            var secondaryData = new object[outParameters.Count];

            for (var j = 0; j < outParameters.Count; j++)
                secondaryData[j] = outParameters[j];

            for (var dynamicParamIndex = 0; dynamicParamIndex < sequenceParameters.Length; dynamicParamIndex++)
            {
                var sp = sequenceParameters[dynamicParamIndex];
                var (j, secondaryFocusEntries) = secondaryFocuses[dynamicParamIndex];
                if (sp.Multiread)
                    secondaryData[j] = sp.Foci.Select(k => secondaryFocusEntries[k.Index].Value.V).ToList();
                else
                    secondaryData[j] = secondaryFocusEntries[sp.Foci[0].Index].Value.V;
            }
            var v = converter.Convert(primaryData.V, secondaryData);

            context.Focus.SequenceIdCounter++;
            result.Add(new FocusEntry
            {
                Key = new ValueWrapper(data.StringAcc),
                Value = new ValueWrapper(v),
                SequenceId = context.Focus.SequenceIdCounter,
            });

            context.Focus.Sequences[sequenceId].ChildrenIds.Add(context.Focus.SequenceIdCounter);
            context.Focus.Sequences[context.Focus.SequenceIdCounter] = new ParsingSequence { ParentId = sequenceId };
        }

        context.Focus.NextRead(data.Id, result);

#if DEBUG
        context.Focus.ValidateTree();
#endif
    }

    internal static void OperationFunctionConvert2(FplQuery parser, ParsingContext context, ParseOperationData data, IConverterFunction converter) {
        context.ReadTransformValue(data.Id, (w) =>
        {
            context.UpdateReadModule(new ValueWrapper(w));
            object raw;
            if (context.ReadingModule != null)
                raw = context.ReadingModule.ExtractValue(w);
            else
                raw = w;

            return converter.Convert(raw, []);
        });
    }
}