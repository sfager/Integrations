using System.Collections.Generic;

namespace Integrations.EDI
{
    public class Segment
    {
        public string Tag { get; }
        public IReadOnlyList<Element> Elements { get; }
        public string RawText { get; }

        public Segment(string tag, IReadOnlyList<Element> elements, string rawText)
        {
            Tag = tag;
            Elements = elements;
            RawText = rawText;
        }
    }
}
