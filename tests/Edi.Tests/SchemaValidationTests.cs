using NUnit.Framework;
using System.Linq;
using Integrations.EDI;

namespace Edi.Tests
{
    public class SchemaValidationTests
    {
        [Test]
        public void MissingRequiredElement_ProducesDiagnostic()
        {
            var content = "ISA*00*00*ZZ*SENDER*ZZ*RECEIVER*200101*1253*U*00401*000000010*0*P*:~GS*PO*SENDER*RECEIVER*200101*1253*1*X*004010~ST*810~SE*1*0001~GE*1*1~IEA*1*000000010~";

            var doc = X12Document.Parse(content);
            Assert.NotNull(doc);
            var diag = doc.Diagnostics.FirstOrDefault(d => d.Code == "ELEMENT_REQUIRED_MISSING");
            Assert.NotNull(diag, "Expected ELEMENT_REQUIRED_MISSING diagnostic when ST missing required element.");
        }

        [Test]
        public void StrictMode_Throws_OnSchemaWarnings()
        {
            var content = "ISA*00*00*ZZ*SENDER*ZZ*RECEIVER*200101*1253*U*00401*000000011*0*P*:~GS*PO*SENDER*RECEIVER*200101*1253*1*X*004010~ST*810~SE*1*0001~GE*1*1~IEA*1*000000011~";

            Assert.Throws<X12ParseException>(() => X12Document.Parse(content, new ParseOptions { Strict = true }));
        }
    }
}
