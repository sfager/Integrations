using NUnit.Framework;
using Integrations.EDI;

namespace Edi.Tests
{
    public class AzureSchemaLoaderTests
    {
        [Test]
        [Ignore("Requires Azure Storage account and credentials")]
        public void AzureSchemaLoader_CanBeConstructed()
        {
            // This test demonstrates how to use AzureSchemaLoader
            // In a real scenario, you would need:
            // 1. An Azure Storage account
            // 2. A container named "schemas" (or custom name)
            // 3. DefaultAzureCredential configured (env vars, managed identity, etc.)
            
            var loader = new AzureSchemaLoader(
                storageAccountName: "mystorageaccount",
                containerName: "schemas"
            );

            // Use with ParseOptions
            var options = new ParseOptions
            {
                SchemaLoader = loader
            };

            // var doc = X12Document.Parse(content, options);
            
            Assert.IsNotNull(loader);
        }

        [Test]
        public void ISchemaLoader_CanUseLocalSchemaLoader()
        {
            ISchemaLoader loader = new LocalSchemaLoader();
            
            var options = new ParseOptions
            {
                SchemaLoader = loader
            };

            string content = "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *231201*1200*U*00401*000000001*0*P*>~" +
                            "GS*IN*SENDER*RECEIVER*20231201*1200*1*X*004010~" +
                            "ST*810*0001~" +
                            "BIG*20231201*INV123*20231101*PO456~" +
                            "SE*3*0001~" +
                            "GE*1*1~" +
                            "IEA*1*000000001~";

            var doc = X12Document.Parse(content, options);
            
            Assert.IsNotNull(doc);
            Assert.AreEqual(1, doc.Interchanges.Count);
        }

        [Test]
        public void ISchemaLoader_CanUseCustomImplementation()
        {
            // Demonstrate custom schema loader
            ISchemaLoader loader = new TestSchemaLoader();
            
            var options = new ParseOptions
            {
                SchemaLoader = loader
            };

            string content = "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *231201*1200*U*00401*000000001*0*P*>~" +
                            "GS*IN*SENDER*RECEIVER*20231201*1200*1*X*004010~" +
                            "ST*810*0001~" +
                            "BIG*20231201*INV123*20231101*PO456~" +
                            "SE*3*0001~" +
                            "GE*1*1~" +
                            "IEA*1*000000001~";

            var doc = X12Document.Parse(content, options);
            
            Assert.IsNotNull(doc);
        }

        // Custom test schema loader
        private class TestSchemaLoader : ISchemaLoader
        {
            public TransactionSchema? LoadSchema(string release, string transactionSetId)
            {
                // Return null or a test schema
                return null;
            }
        }
    }
}
