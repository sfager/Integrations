using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Integrations.EDI;
using Integrations.EDI.Model;

namespace Integrations.EDI.Parser;

    internal class X12Parser
    {
        private const char DefaultElement = '*';
        private const char DefaultComponent = ':';
        private const char DefaultSegment = '~';
        private const char DefaultRelease = '?';

        public X12Document Parse(string content, ParseOptions? options = null)
        {
            options ??= new ParseOptions();

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("content is null or empty", nameof(content));

            var diagnostics = new List<ParseWarning>();

            var trimmed = content.Trim();
            DetectDelimiters(trimmed, out char elementSep, out char componentSep, out char segmentTerm,
                out char releaseChar);

            var segmentsRaw = SplitSegments(trimmed, segmentTerm, releaseChar);

            var segments = new List<Segment>();
            foreach (var seg in segmentsRaw)
            {
                if (string.IsNullOrWhiteSpace(seg))
                    continue;
                // Extract tag: up to first element separator or first 3 chars
                var tag = ExtractTag(seg, elementSep);
                var rest = seg.Length > tag.Length ? seg.Substring(tag.Length).TrimStart(elementSep) : string.Empty;

                var elementValues = SplitRespectingRelease(rest, elementSep, releaseChar);
                var elements = new List<Element>();
                foreach (var ev in elementValues)
                {
                    if (string.IsNullOrEmpty(ev))
                    {
                        elements.Add(new Element(string.Empty));
                        continue;
                    }

                    var components = SplitRespectingRelease(ev, componentSep, releaseChar);
                    if (components.Count > 1)
                        elements.Add(new Element(ev, components));
                    else
                        elements.Add(new Element(ev));
                }

                segments.Add(new Segment(tag, elements, seg));
            }

            // Build X12 envelope grouping: ISA..IEA > GS..GE > ST..SE
            var interchanges = new List<Interchange>();
            Interchange? currentIsa = null;
            FunctionalGroup? currentGs = null;
            TransactionSet? currentSt = null;

            for (int i = 0; i < segments.Count; i++)
            {
                var s = segments[i];
                var tag = s.Tag.ToUpperInvariant();
                if (tag == "ISA")
                {
                    // start new interchange
                    var isaHeader = ParseIsaHeader(s);
                    currentIsa = new Interchange(s.RawText, isaHeader, new List<FunctionalGroup>());
                    interchanges.Add(currentIsa);
                    currentGs = null;
                    currentSt = null;
                    continue;
                }

                if (tag == "IEA")
                {
                    // end of interchange
                    currentIsa = null;
                    currentGs = null;
                    currentSt = null;
                    continue;
                }

                if (tag == "GS")
                {
                    var gsHeader = ParseGsHeader(s);
                    var fg = new FunctionalGroup(s.RawText, gsHeader, new List<TransactionSet>());
                    if (currentIsa != null)
                    {
                        ((List<FunctionalGroup>)currentIsa.FunctionalGroups).Add(fg);
                    }

                    currentGs = fg;
                    currentSt = null;
                    continue;
                }

                if (tag == "GE")
                {
                    // end of functional group
                    currentGs = null;
                    currentSt = null;
                    continue;
                }

                if (tag == "ST")
                {
                    var stHeader = ParseStHeader(s);
                    var tsSegments = new List<Segment> { s };
                    var ts = new TransactionSet(s.RawText, stHeader, tsSegments);
                    if (currentGs != null)
                    {
                        ((List<TransactionSet>)currentGs.TransactionSets).Add(ts);
                    }

                    currentSt = ts;
                    continue;
                }

                if (tag == "SE")
                {
                    // close current transaction if any; include SE segment in its segments
                    if (currentSt != null)
                    {
                        ((List<Segment>)currentSt.Segments).Add(s);
                    }

                    currentSt = null;
                    continue;
                }

// Default: add segment to current transaction if present
                if (currentSt != null)
                {
                    ((List<Segment>)currentSt.Segments).Add(s);
                }
            }

            // Post-parse validations: produce diagnostics. Do not throw unless strict mode enabled.
            // Validate ISA..IEA declared functional group counts
            for (int i = 0; i < segments.Count; i++)
            {
                var s = segments[i];
                var tag = s.Tag.ToUpperInvariant();
                if (tag == "ISA")
                {
                    // find matching IEA
                    int isaIdx = i;
                    int ieaIdx = -1;
                    for (int j = isaIdx + 1; j < segments.Count; j++)
                    {
                        if (segments[j].Tag.ToUpperInvariant() == "IEA")
                        {
                            ieaIdx = j;
                            break;
                        }
                    }

                    if (ieaIdx >= 0)
                    {
                        var ieaSeg = segments[ieaIdx];
                        var declared = ieaSeg.Elements.Count > 0 ? ieaSeg.Elements[0].RawValue : null;
                        // count GS between isaIdx and ieaIdx
                        int actualGs = 0;
                        for (int j = isaIdx + 1; j < ieaIdx; j++)
                            if (segments[j].Tag.ToUpperInvariant() == "GS") actualGs++;

                        if (!string.IsNullOrEmpty(declared) && int.TryParse(declared, out var declaredInt))
                        {
                            if (declaredInt != actualGs)
                            {
                                diagnostics.Add(new ParseWarning("IEA_COUNT_MISMATCH", $"IEA declares {declaredInt} functional groups but found {actualGs} between ISA at index {isaIdx} and IEA at index {ieaIdx}.") );
                            }
                        }
                    }
                    else
                    {
                        diagnostics.Add(new ParseWarning("MISSING_IEA", $"ISA at index {isaIdx} has no matching IEA segment."));
                    }
                }

                if (tag == "GS")
                {
                    int gsIdx = i;
                    int geIdx = -1;
                    for (int j = gsIdx + 1; j < segments.Count; j++)
                    {
                        if (segments[j].Tag.ToUpperInvariant() == "GE")
                        {
                            geIdx = j;
                            break;
                        }
                    }

                    if (geIdx >= 0)
                    {
                        var geSeg = segments[geIdx];
                        var declared = geSeg.Elements.Count > 0 ? geSeg.Elements[0].RawValue : null;
                        // count ST between gsIdx and geIdx
                        int actualSt = 0;
                        for (int j = gsIdx + 1; j < geIdx; j++)
                            if (segments[j].Tag.ToUpperInvariant() == "ST") actualSt++;

                        if (!string.IsNullOrEmpty(declared) && int.TryParse(declared, out var declaredInt))
                        {
                            if (declaredInt != actualSt)
                            {
                                diagnostics.Add(new ParseWarning("GE_COUNT_MISMATCH", $"GE declares {declaredInt} transaction sets but found {actualSt} between GS at index {gsIdx} and GE at index {geIdx}.") );
                            }
                        }
                    }
                    else
                    {
                        diagnostics.Add(new ParseWarning("MISSING_GE", $"GS at index {gsIdx} has no matching GE segment."));
                    }
                }
            }

            // Validate each TransactionSet for missing SE
            foreach (var interchange in interchanges)
            {
                foreach (var fg in interchange.FunctionalGroups)
                {
                    foreach (var ts in fg.TransactionSets)
                    {
                        var lastTag = ts.Segments.Count > 0 ? ts.Segments.Last().Tag.ToUpperInvariant() : string.Empty;
                        if (lastTag != "SE")
                        {
                            diagnostics.Add(new ParseWarning("MISSING_SE", $"Transaction set {ts.ST.TransactionSetControlNumber} (ST {ts.ST.TransactionSetId}) is missing closing SE segment."));
                        }
                    }
                }
            }

            // Schema-based validation: load schemas and validate transactions
            try
            {
                var loader = options?.SchemaLoader ?? new LocalSchemaLoader(options?.SchemaDirectory);

                foreach (var interchange in interchanges)
                {
                    // Try to determine release from ISA version (ISA12)
                    var release = "4010"; // default
                    if (interchange.ISA.ControlNumber.Length > 0)
                    {
                        // In a real implementation, you'd extract version from ISA segment
                        // For now, use default
                    }

                    foreach (var fg in interchange.FunctionalGroups)
                    {
                        var txList = (List<TransactionSet>)fg.TransactionSets;
                        for (int i = 0; i < txList.Count; i++)
                        {
                            var ts = txList[i];
                            var txSchema = loader.LoadSchema(release, ts.ST.TransactionSetId);
                            var vr = SchemaValidator.ValidateTransaction(ts, txSchema);
                            if (vr.Warnings.Count > 0)
                                diagnostics.AddRange(vr.Warnings);

                            if (vr.Loops.Count > 0)
                            {
                                // replace transaction with one that includes loops
                                var newTs = new TransactionSet(ts.RawST, ts.ST, ts.Segments.ToList(), vr.Loops);
                                txList[i] = newTs;
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore schema loading errors; schema is optional
            }

            if (options.Strict && diagnostics.Count > 0)
            {
                throw new X12ParseException("Strict mode detected parse issues.", diagnostics);
            }

            return new X12Document(segments, interchanges, diagnostics);
        }

        private static string ExtractTag(string seg, char elementSep)
        {
            var idx = seg.IndexOf(elementSep);
            if (idx <= 0)
            {
                // fallback: first up-to-3 letters
                return seg.Length >= 3 ? seg.Substring(0, 3) : seg;
            }

            return seg.Substring(0, idx);
        }

        private static ISAHeader ParseIsaHeader(Segment seg)
        {
            // elements: ISA01..ISA16 after tag; use common positions (tolerant)
            var els = seg.Elements;
            string authInfo = els.Count > 1 ? els[1].RawValue : string.Empty; // ISA02 (best effort)
            string secInfo = els.Count > 3 ? els[3].RawValue : string.Empty; // ISA04 (best effort)
            string sender = string.Empty;
            string receiver = string.Empty;
            if (els.Count > 7)
            {
                sender = els[5].RawValue; // ISA06
                receiver = els[7].RawValue; // ISA08
            }
            else if (els.Count > 5)
            {
                // tolerant fallback when some qualifiers are omitted
                sender = els[3].RawValue;
                receiver = els[5].RawValue;
            }
            else if (els.Count > 1)
            {
                sender = els[1].RawValue;
                receiver = els.Count > 2 ? els[2].RawValue : string.Empty;
            }

            string date =
                els.Count > 8 ? els[8].RawValue : (els.Count > 6 ? els[6].RawValue : string.Empty); // ISA09 or fallback
            string time =
                els.Count > 9 ? els[9].RawValue : (els.Count > 7 ? els[7].RawValue : string.Empty); // ISA10 or fallback
            string ctrl =
                els.Count > 12 ? els[12].RawValue : (els.Count > 10 ? els[10].RawValue : string.Empty); // ISA13 or fallback
            return new ISAHeader(authInfo, secInfo, sender, receiver, date, time, ctrl);
        }

        private static GSHeader ParseGsHeader(Segment seg)
        {
            var els = seg.Elements;
            string functionalId = els.Count > 0 ? els[0].RawValue : string.Empty; // GS01
            string appSender = els.Count > 1 ? els[1].RawValue : string.Empty; // GS02
            string appReceiver = els.Count > 2 ? els[2].RawValue : string.Empty; // GS03
            string date = els.Count > 3 ? els[3].RawValue : string.Empty; // GS04
            string time = els.Count > 4 ? els[4].RawValue : string.Empty; // GS05
            string ctrl = els.Count > 5 ? els[5].RawValue : string.Empty; // GS06
            return new GSHeader(functionalId, appSender, appReceiver, date, time, ctrl);
        }

        private static STHeader ParseStHeader(Segment seg)
        {
            var els = seg.Elements;
            string tid = els.Count > 0 ? els[0].RawValue : string.Empty; // ST01
            string ctrl = els.Count > 1 ? els[1].RawValue : string.Empty; // ST02
            return new STHeader(tid, ctrl);
        }

        private static void DetectDelimiters(string content, out char elementSep, out char componentSep,
            out char segmentTerm, out char releaseChar)
        {
            elementSep = DefaultElement;
            componentSep = DefaultComponent;
            segmentTerm = DefaultSegment;
            releaseChar = DefaultRelease;

            if (content.StartsWith("UNA"))
            {
                // EDIFACT UNA segment: UNA<component><element><decimal><release><reserved><segment>
                if (content.Length >= 9)
                {
                    componentSep = content[3];
                    elementSep = content[4];
                    // decimal = content[5];
                    releaseChar = content[6];
                    // reserved = content[7];
                    segmentTerm = content[8];
                }

                return;
            }

            if (content.StartsWith("ISA"))
            {
                // X12: element separator is the char at position 3 (0-based index 3)
                if (content.Length > 3)
                    elementSep = content[3];
                // component/sub-element separator commonly at position 105 (index 104)
                if (content.Length > 104)
                    componentSep = content[104];
                // segment terminator often at the end of segments; keep default
                return;
            }

            if (content.StartsWith("UNB") || content.StartsWith("UNH"))
            {
                // EDIFACT-like: typically element separator comes after tag (e.g., UNB+...)
                if (content.Length > 3)
                    elementSep = content[3];
                return;
            }

            // fallback: keep defaults
        }

        private static List<string> SplitSegments(string content, char segmentTerm, char releaseChar)
        {
            var list = new List<string>();
            var sb = new StringBuilder();
            bool inRelease = false;
            foreach (var c in content)
            {
                if (inRelease)
                {
                    sb.Append(c);
                    inRelease = false;
                    continue;
                }

                if (c == releaseChar)
                {
                    inRelease = true;
                    continue;
                }

                if (c == segmentTerm)
                {
                    list.Add(sb.ToString());
                    sb.Clear();
                    continue;
                }

                sb.Append(c);
            }

            // add last if any
            if (sb.Length > 0)
                list.Add(sb.ToString());
            return list;
        }

        private static List<string> SplitRespectingRelease(string text, char sep, char releaseChar)
        {
            var list = new List<string>();
            var sb = new StringBuilder();
            bool inRelease = false;
            foreach (var c in text)
            {
                if (inRelease)
                {
                    sb.Append(c);
                    inRelease = false;
                    continue;
                }

                if (c == releaseChar)
                {
                    inRelease = true;
                    continue;
                }

                if (c == sep)
                {
                    list.Add(sb.ToString());
                    sb.Clear();
                    continue;
                }

                sb.Append(c);
            }

            list.Add(sb.ToString());
            return list;
        }
    }