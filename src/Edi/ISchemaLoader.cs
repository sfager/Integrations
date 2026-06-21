namespace Integrations.EDI
{
    public interface ISchemaLoader
    {
        TransactionSchema? LoadSchema(string release, string transactionSetId);
    }
}
