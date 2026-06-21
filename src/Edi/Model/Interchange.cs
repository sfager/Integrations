using System.Collections.Generic;
using Integrations.EDI.Model;

namespace Integrations.EDI
{
    public class Interchange
    {
        public string RawISA { get; }
        public ISAHeader ISA { get; }
        public IReadOnlyList<FunctionalGroup> FunctionalGroups { get; }
        public Interchange(string rawIsa, ISAHeader isa, List<FunctionalGroup>? groups = null)
        {
            RawISA = rawIsa;
            ISA = isa;
            FunctionalGroups = groups ?? new List<FunctionalGroup>();
        }
    }
}
