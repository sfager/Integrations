using System;
using System.Collections.Generic;
using System.Linq;

namespace Integrations.EDI
{
    public class SchemaValidationResult
    {
        public List<ParseWarning> Warnings { get; } = new List<ParseWarning>();
        public List<Loop> Loops { get; } = new List<Loop>();
    }

    public class SchemaValidator
    {
        public static SchemaValidationResult ValidateTransaction(TransactionSet ts, TransactionSchema? txSchema)
        {
            var result = new SchemaValidationResult();
            if (txSchema == null)
            {
                // no schema available for this transaction
                return result;
            }

            // Validate segments for required elements (simple rule: for segments matching ST tag definitions)
            foreach (var seg in ts.Segments)
            {
                var tag = seg.Tag.ToUpperInvariant();
                // special-case: validate ST segment itself
                if (tag == "ST")
                {
                    foreach (var elDef in txSchema.Elements)
                    {
                        int pos = elDef.Position - 1; // schema positions are 1-based
                        if (seg.Elements.Count <= pos || string.IsNullOrEmpty(seg.Elements[pos].RawValue))
                        {
                            if (elDef.Required)
                            {
                                result.Warnings.Add(new ParseWarning("ELEMENT_REQUIRED_MISSING", $"Segment {tag} is missing required element {elDef.Name} at position {elDef.Position} in transaction {ts.ST.TransactionSetId}."));
                            }
                        }
                        else
                        {
                            // type check
                            var val = seg.Elements[pos].RawValue;
                            if (elDef.Type == "N")
                            {
                                if (!long.TryParse(val, out _))
                                    result.Warnings.Add(new ParseWarning("ELEMENT_TYPE_MISMATCH", $"Segment {tag} element {elDef.Name} expected numeric but got '{val}'."));
                            }
                            // AN accepts any
                        }
                    }
                }
            }

            // Loop detection: naive approach using anchor tags
            foreach (var loopDef in txSchema.Loops)
            {
                var loop = new Loop(loopDef.Name);
                int idx = 0;
                while (idx < ts.Segments.Count)
                {
                    var seg = ts.Segments[idx];
                    if (string.Equals(seg.Tag, loopDef.Anchor, StringComparison.OrdinalIgnoreCase))
                    {
                        var instanceSegs = new List<Segment> { seg };
                        idx++;
                        // gather until next anchor or end
                        while (idx < ts.Segments.Count && !string.Equals(ts.Segments[idx].Tag, loopDef.Anchor, StringComparison.OrdinalIgnoreCase))
                        {
                            instanceSegs.Add(ts.Segments[idx]);
                            idx++;
                        }

                        loop.Instances.Add(new LoopInstance(instanceSegs));
                    }
                    else
                    {
                        idx++;
                    }
                }

                if (loop.Instances.Count > loopDef.MaxUse)
                {
                    result.Warnings.Add(new ParseWarning("LOOP_MAXUSE_EXCEEDED", $"Loop {loop.Name} exceeded max use {loopDef.MaxUse} with {loop.Instances.Count} instances."));
                }

                if (loop.Instances.Count > 0)
                    result.Loops.Add(loop);
            }

            return result;
        }
    }
}
