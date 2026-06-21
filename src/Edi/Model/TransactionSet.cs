using System.Collections.Generic;

namespace Integrations.EDI
{
    public class TransactionSet
    {
        public string RawST { get; }
        public STHeader ST { get; }
        public IReadOnlyList<Segment> Segments { get; }
public IReadOnlyList<Loop> Loops { get; }

public TransactionSet(string rawSt, STHeader st, List<Segment>? segments = null, List<Loop>? loops = null)
{
    RawST = rawSt;
    ST = st;
    Segments = segments ?? new List<Segment>();
    Loops = loops ?? new List<Loop>();
}
    }
}
