namespace Integrations.EDI
{
    public class ParseWarning
    {
        public string Code { get; }
        public string Message { get; }

        public ParseWarning(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public override string ToString() => $"{Code}: {Message}";
    }
}
