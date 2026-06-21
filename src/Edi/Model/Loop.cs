using System.Collections.Generic;

namespace Integrations.EDI
{
    public class LoopInstance
    {
        public IReadOnlyList<Segment> Segments { get; }
        public LoopInstance(List<Segment> segments)
        {
            Segments = segments;
        }
    }

    public class Loop
    {
        public string Name { get; }
        public List<LoopInstance> Instances { get; }

        public Loop(string name)
        {
            Name = name;
            Instances = new List<LoopInstance>();
        }
    }
}
