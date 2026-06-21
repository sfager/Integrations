using System.Collections.Generic;

namespace Integrations.EDI.Model;

public class Element
{
    public string RawValue { get; }
    public IReadOnlyList<string>? Components { get; }
    public bool IsComposite => Components != null && Components.Count > 1;

    public Element(string rawValue, IReadOnlyList<string>? components = null)
    {
        RawValue = rawValue;
        Components = components;
    }
}