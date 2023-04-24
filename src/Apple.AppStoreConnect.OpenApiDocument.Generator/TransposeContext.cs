using System.Collections.Generic;
using System.Linq;

namespace Apple.AppStoreConnect.OpenApiDocument.Generator;

public sealed class TransposeContext
{
    private readonly IDictionary<string, string> _enumComponents = new Dictionary<string, string>();

    private readonly IDictionary<string, IReadOnlyCollection<string>> _enumComponentValues =
        new Dictionary<string, IReadOnlyCollection<string>>();

    public string GetEnumComponentReference(IReadOnlyCollection<string> enumValues)
    {
        var key = string.Join(",", enumValues.OrderBy(x => x));

        if (!_enumComponents.TryGetValue(key, out var componentSchema))
        {
            componentSchema = "";
            _enumComponents.Add(key, componentSchema);
            _enumComponentValues.Add(key, enumValues);
        }

        return $"#/components/schemas/{componentSchema}";
    }
}
