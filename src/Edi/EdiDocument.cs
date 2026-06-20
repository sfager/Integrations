using System;
using System.Collections.Generic;
using System.Linq;

namespace Integrations.EDI
{
    public class EdiDocument
    {
        public IReadOnlyList<Segment> Segments { get; }

        internal EdiDocument(List<Segment> segments)
        {
            Segments = segments;
        }

        public static EdiDocument Parse(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("content is null or empty", nameof(content));

            return new Parser.EdiParser().Parse(content);
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
