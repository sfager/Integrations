namespace Integrations.EDI
{
    public record ISAHeader(string AuthorizationInfo, string SecurityInfo, string InterchangeSender, string InterchangeReceiver, string Date, string Time, string ControlNumber);
    public record GSHeader(string FunctionalIdCode, string ApplicationSender, string ApplicationReceiver, string Date, string Time, string ControlNumber);
    public record STHeader(string TransactionSetId, string TransactionSetControlNumber);
}
