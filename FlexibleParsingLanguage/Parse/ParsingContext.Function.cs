using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Parse
{
    internal partial class ParsingContext
    {

        internal void OperationFunctionConvertMultiParam(FplQuery parser, ParseOperationData data, List<CompiledParameter> parameters, Func<object, object[], object> converter)
        {
#if DEBUG
            this.Focus.ValidateTree();
#endif

            var primaryFocus = this.Focus.Reads[this.Focus.Active.ReadId];

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

                var node = this.Focus.Store[p.Id];
                var secFocus = this.Focus.Reads[node.ReadId];
                secondarySequences.Add(secFocus.Select(x => x.SequenceId).ToList());
                secondaryFocuses.Add((i, secFocus));
            }

            var sequenceIntersection = this.Focus.GenerateSequencesIntersectionInner(primarySequences, secondarySequences.ToArray());

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

                var secondaryData = new object[outParameters.Count];

                for (var j = 0; j < outParameters.Count; j++)
                    secondaryData[j] = outParameters[j];

                for (var dynamicParamIndex = 0; dynamicParamIndex < sequenceParameters.Length; dynamicParamIndex++)
                {
                    var sp = sequenceParameters[dynamicParamIndex];
                    var (j, secondaryFocusEntries) = secondaryFocuses[dynamicParamIndex];
                    if (sp.Multiread)
                        secondaryData[j] = sp.Foci.Select(k => ExtractReadValue(secondaryFocusEntries[k.Index])).ToList();
                    else
                        secondaryData[j] = ExtractReadValue(secondaryFocusEntries[sp.Foci[0].Index]);
                }

                var v = converter.Invoke(ExtractReadValue(primaryFocus[i]), secondaryData);

                result.Add(new FocusEntry
                {
                    Key = new ValueWrapper(data.StringAcc),
                    Value = new ValueWrapper(v),
                    SequenceId = sequenceId,
                });
            }

            this.Focus.NextRead(data.Id, result);

#if DEBUG
            this.Focus.ValidateTree();
#endif
        }


        private object ExtractReadValue(FocusEntry w) => GetReadingModule(w.Value).ExtractValue(w.Value.V);


        /*

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

        */

    }
}
