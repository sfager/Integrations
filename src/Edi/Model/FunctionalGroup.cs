using System.Collections.Generic;

namespace Integrations.EDI.Model;

public class FunctionalGroup
{
    public string RawGS { get; }
    public GSHeader GS { get; }
    public IReadOnlyList<TransactionSet> TransactionSets { get; }

    public FunctionalGroup(string rawGs, GSHeader gs, List<TransactionSet>? tss = null)
    {
        RawGS = rawGs;
        GS = gs;
        TransactionSets = tss ?? new List<TransactionSet>();
    }
}

