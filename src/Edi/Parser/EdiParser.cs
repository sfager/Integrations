using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Integrations.EDI.Parser
{
    using Integrations.EDI;

    internal class EdiParser
    {
        private const char DefaultElement = '*';
        private const char DefaultComponent = ':';
        private const char DefaultSegment = '~';
        private const char DefaultRelease = '?';

        public EdiDocument Parse(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("content is null or empty", nameof(content));

            var trimmed = content.Trim();
            DetectDelimiters(trimmed, out char elementSep, out char componentSep, out char segmentTerm, out char releaseChar);

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

            return new EdiDocument(segments);
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

        private static void DetectDelimiters(string content, out char elementSep, out char componentSep, out char segmentTerm, out char releaseChar)
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
}
