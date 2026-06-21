using System;
using System.Collections.Generic;

namespace Integrations.EDI
{
    public class X12ParseException : Exception
    {
        public IReadOnlyList<ParseWarning> Diagnostics { get; }

        public X12ParseException(string message, List<ParseWarning> diagnostics) : base(message)
        {
            Diagnostics = diagnostics ?? new List<ParseWarning>();
        }
    }
}
