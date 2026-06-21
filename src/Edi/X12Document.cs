using System;
using System.Collections.Generic;
using System.Linq;

namespace Integrations.EDI
{
    public class X12Document
    {
        // Low-level segments (kept for compatibility/low-level access)
        public IReadOnlyList<Segment> Segments { get; }
        // Higher level envelope structure (populated by X12Parser in later phases)
        public IReadOnlyList<Interchange> Interchanges { get; }

        internal X12Document(List<Segment> segments, List<Interchange>? interchanges = null, List<ParseWarning>? diagnostics = null)
        {
            Segments = segments;
            Interchanges = interchanges ?? new List<Interchange>();
            Diagnostics = diagnostics ?? new List<ParseWarning>();
        }

        public IReadOnlyList<ParseWarning> Diagnostics { get; }

        public static X12Document Parse(string content, ParseOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("content is null or empty", nameof(content));

            return new Parser.X12Parser().Parse(content, options);
        }

        public IEnumerable<Segment> GetSegments(string tag)
        {
            return Segments.Where(s => string.Equals(s.Tag, tag, StringComparison.OrdinalIgnoreCase));
        }

        public Segment? GetFirst(string tag)
        {
            return GetSegments(tag).FirstOrDefault();
        }
    }
}
