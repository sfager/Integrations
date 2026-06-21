using NUnit.Framework;
using System.Linq;
using Integrations.EDI;

namespace Edi.Tests
{
    public class SchemaLoaderTests
    {
        [Test]
        public void LoadSchema_WithTransactionSetId_ReturnsSchema()
        {
            var loader = new LocalSchemaLoader();
            var schema = loader.LoadSchema("4010", "810");
            
            Assert.NotNull(schema);
            Assert.AreEqual("810", schema!.TransactionSetId);
            Assert.IsTrue(schema.Elements.Count > 0);
        }

        [Test]
        public void LoadSchema_CachesResults()
        {
            var loader = new LocalSchemaLoader();
            var schema1 = loader.LoadSchema("4010", "810");
            var schema2 = loader.LoadSchema("4010", "810");
            
            Assert.AreSame(schema1, schema2);
        }
    }
}
