using System.Linq;
using NUnit.Framework;
using Integrations.EDI;

namespace Edi.Tests
{
    public class EdiParserTests
    {
        [Test]
        public void Parse_WithISA_DerivesElementSeparatorAndParsesComposite()
        {
            // ISA at start; element separator is char at index 3 ('*')
            var content = "ISA*00*00~SEG*val1:comp2*val2~DEF*X~";

            var doc = EdiDocument.Parse(content);

            var seg = doc.GetFirst("SEG");
            Assert.NotNull(seg);
            var elements = seg!.Elements.ToList();
            Assert.IsTrue(elements[0].IsComposite);
            Assert.AreEqual("val1", elements[0].Components![0]);
            Assert.AreEqual("comp2", elements[0].Components![1]);
            Assert.AreEqual("val2", elements[1].RawValue);
        }

        [Test]
        public void Parse_WithUNA_EDIFACT_DetectsDelimitersAndParses()
        {
            // UNA: component:+ element:+ decimal:. release:? reserved: space segment:' (apostrophe)
            var content = "UNA:+.? 'UNB+1:2+3'SEG+AA:BB+CC'";

            var doc = EdiDocument.Parse(content);

            var segUnb = doc.GetFirst("UNB");
            Assert.NotNull(segUnb);
            var unbEls = segUnb!.Elements.ToList();
            // first element should be "1:2" split into components 1 and 2
            Assert.IsTrue(unbEls[0].IsComposite);
            Assert.AreEqual("1", unbEls[0].Components![0]);
            Assert.AreEqual("2", unbEls[0].Components![1]);

            var seg = doc.GetFirst("SEG");
            Assert.NotNull(seg);
            var segEls = seg!.Elements.ToList();
            Assert.AreEqual("AA", segEls[0].Components![0]);
            Assert.AreEqual("BB", segEls[0].Components![1]);
            Assert.AreEqual("CC", segEls[1].RawValue);
        }

        [Test]
        public void Parse_ReleaseChar_EscapesElementSeparator()
        {
            // element separator '*' escaped by '?' so it is part of the value
            var content = "ISA*00*00~SEG*val1?*withstar*val2~";

            var doc = EdiDocument.Parse(content);
            var seg = doc.GetFirst("SEG");
            Assert.NotNull(seg);
            var els = seg!.Elements.ToList();
            Assert.IsTrue(els.Count >= 2);
            Assert.IsTrue(els[0].RawValue.Contains("val1"));
            Assert.AreEqual("val2", els.Last().RawValue);
        }

        [Test]
        public void Parse_ReleaseChar_EscapesSegmentTerminator()
        {
            // release '?' before segment terminator '~' should keep '~' as literal inside element value
            var content = "ISA*00*00~SEG*first?~part*second~";

            var doc = EdiDocument.Parse(content);
            var seg = doc.GetFirst("SEG");
            Assert.NotNull(seg);
            var els = seg!.Elements.ToList();
            Assert.AreEqual(2, els.Count);
            Assert.AreEqual("first~part", els[0].RawValue);
            Assert.AreEqual("second", els[1].RawValue);
        }

        [Test]
        public void Parse_MalformedEmptyContent_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => EdiDocument.Parse(string.Empty));
        }

        [Test]
        public void Parse_VaryingElementCounts_Tolerant()
        {
            var content = "SEG*1~SEG*1*2*3~";
            var doc = EdiDocument.Parse(content);
            var segs = doc.GetSegments("SEG").ToList();
            Assert.AreEqual(2, segs.Count);
            Assert.AreEqual(1, segs[0].Elements.Count);
            Assert.AreEqual(3, segs[1].Elements.Count);
        }
    }
}
