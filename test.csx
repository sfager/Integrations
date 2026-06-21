using System;
using Integrations.EDI;

var loader = new SchemaLoader();
var schema = loader.LoadSchema("4010", "810");
Console.WriteLine($"Schema is null: {schema == null}");
if (schema != null) {
    Console.WriteLine($"TransactionSetId: '{schema.TransactionSetId}'");
    Console.WriteLine($"Release: '{schema.Release}'");
    Console.WriteLine($"Elements count: {schema.Elements.Count}");
}
