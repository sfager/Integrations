using NUnit.Framework;
using Integrations.EDI;
using System.Linq;

namespace Edi.Tests
{
    public class X12ParserTests
    {
        [Test]
        public void Parse_FullInterchange_GroupsIntoEnvelopes()
        {
            var content = "ISA*00*00*ZZ*SENDER*ZZ*RECEIVER*200101*1253*U*00401*000000001*0*P*:~GS*PO*SENDER*RECEIVER*200101*1253*1*X*004010~ST*850*0001~N1*XYZ~SE*2*0001~GE*1*1~IEA*1*000000001~";

            var doc = X12Document.Parse(content);

Assert.NotNull(doc);
            Assert.IsNotEmpty(doc.Interchanges);
            var interchange = doc.Interchanges.First();
            var isaConcat = string.Join('|', new[] { interchange.ISA.AuthorizationInfo, interchange.ISA.SecurityInfo, interchange.ISA.InterchangeSender, interchange.ISA.InterchangeReceiver, interchange.ISA.Date, interchange.ISA.Time, interchange.ISA.ControlNumber });
            Assert.IsTrue(isaConcat.Contains("SENDER") && isaConcat.Contains("RECEIVER"));
            Assert.IsNotEmpty(interchange.FunctionalGroups);
            var fg = interchange.FunctionalGroups.First();
            Assert.IsNotEmpty(fg.TransactionSets);
            var ts = fg.TransactionSets.First();
            // ST should be first segment in transaction set segments
            Assert.AreEqual("ST", ts.Segments.First().Tag.ToUpperInvariant());
            // SE should be present as last
            Assert.AreEqual("SE", ts.Segments.Last().Tag.ToUpperInvariant());
        }
    }
}
