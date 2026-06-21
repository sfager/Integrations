namespace Integrations.EDI
{
    public class ParseOptions
    {
        public bool Strict { get; set; } = false;
        public string? SchemaDirectory { get; set; } = null;
        public ISchemaLoader? SchemaLoader { get; set; } = null;
    }
}
