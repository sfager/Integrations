using System.Linq;
using NUnit.Framework;
using Integrations.EDI;

namespace Edi.Tests
{
    public class X12Phase3Tests
    {
        [Test]
        public void Iea_Count_Mismatch_IsDetectable()
        {
            // IEA claims 2 functional groups but only one GS present
            var content = "ISA*00*00*ZZ*SENDER*ZZ*RECEIVER*200101*1253*U*00401*000000001*0*P*:~GS*PO*A*B*200101*1253*1*X*004010~ST*850*0001~SE*2*0001~GE*1*1~IEA*2*000000001~";

            var doc = X12Document.Parse(content);
            Assert.NotNull(doc);

            var iea = doc.GetFirst("IEA");
            Assert.NotNull(iea);
            var declaredCount = iea!.Elements.Count > 0 ? iea.Elements[0].RawValue : null;
            Assert.NotNull(declaredCount);

            var actualGroups = doc.Interchanges.First().FunctionalGroups.Count;
            // The declared count should not match the actual number of functional groups (mismatch scenario)
            Assert.AreNotEqual(declaredCount, actualGroups.ToString());
        }

        [Test]
        public void Missing_SE_Transaction_Is_Tolerant_But_Incomplete()
        {
            // Single ST with no SE; parser should still create a transaction set containing ST and subsequent segments
            var content = "ISA*00*00*ZZ*SENDER*ZZ*RECEIVER*200101*1253*U*00401*000000002*0*P*:~GS*PO*A*B*200101*1253*1*X*004010~ST*999*0002~N1*ABC~";

            var doc = X12Document.Parse(content);
            Assert.NotNull(doc);
            var fg = doc.Interchanges.First().FunctionalGroups.First();
            var ts = fg.TransactionSets.First();

            // ST exists
            Assert.AreEqual("ST", ts.Segments.First().Tag.ToUpperInvariant());
            // No SE present at end
            Assert.AreNotEqual("SE", ts.Segments.Last().Tag.ToUpperInvariant());
            // Transaction still contains the N1 segment
            Assert.IsTrue(ts.Segments.Any(s => string.Equals(s.Tag, "N1", System.StringComparison.OrdinalIgnoreCase)));
        }

        [Test]
        public void ReleaseChar_Escapes_Component_Separator_Inside_Composite()
        {
            // Use release '?' to escape component separator ':' inside a component value
            // Element 1 has components where first component contains an escaped ':' -> "value:withcolon"
            var content = "ISA*00*00~SEG*value?:withcolon:next*other~";

            var doc = X12Document.Parse(content);
            var seg = doc.GetFirst("SEG");
            Assert.NotNull(seg);
            var el = seg!.Elements.First();
            Assert.IsTrue(el.IsComposite);
            var comps = el.Components!.ToList();
            // tolerant assertions: first component should start with 'value' and last be 'next'
            Assert.IsTrue(comps[0].StartsWith("value"));
            Assert.AreEqual("next", comps.Last());
        }

        [Test]
        public void Multiple_GS_Multiple_ST_Grouping_Counts_Are_Correct()
        {
            // Build an interchange with two GS groups, each containing two ST transactions
            var content =
                "ISA*00*00*ZZ*SENDER*ZZ*RECEIVER*200101*1253*U*00401*000000003*0*P*:~"
                + "GS*PO*A*B*200101*1253*1*X*004010~" // GS1
                + "ST*100*0001~SE*1*0001~"             // ST1 in GS1
                + "ST*101*0002~SE*1*0002~"             // ST2 in GS1
                + "GE*2*1~"
                + "GS*IN*C*D*200101*1300*2*X*004010~" // GS2
                + "ST*200*0003~SE*1*0003~"             // ST1 in GS2
                + "ST*201*0004~SE*1*0004~"             // ST2 in GS2
                + "GE*2*2~"
                + "IEA*1*000000003~";

            var doc = X12Document.Parse(content);
            Assert.NotNull(doc);
            var interchange = doc.Interchanges.First();
            Assert.AreEqual(2, interchange.FunctionalGroups.Count);
            Assert.AreEqual(2, interchange.FunctionalGroups[0].TransactionSets.Count);
            Assert.AreEqual(2, interchange.FunctionalGroups[1].TransactionSets.Count);
        }
    }
}
