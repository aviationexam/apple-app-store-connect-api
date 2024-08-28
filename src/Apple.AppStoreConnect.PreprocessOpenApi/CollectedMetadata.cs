using System.Text;

namespace Apple.AppStoreConnect.PreprocessOpenApi;

public sealed class CollectedMetadata
{
    private Version? _targetApiVersion;
    private readonly Dictionary<string, ICollection<string>> _discriminatorData = new();
    private readonly Dictionary<string, string?> _components = new();
    private readonly HashSet<string> _deprecatedComponents = [];

    public void AddVersion(ReadOnlySpan<byte> target)
    {
        _targetApiVersion = Version.Parse(Encoding.UTF8.GetString(target));
    }

    public void AddOneOf(
        Stack<TreeItem> currentPath, ReadOnlySpan<byte> target
    )
    {
        var path = currentPath.Skip(2).Reverse().Collect();

        if (!_discriminatorData.TryGetValue(path, out var targets))
        {
            targets = new List<string>();
            _discriminatorData.Add(path, targets);
        }

        var targetString = Encoding.UTF8.GetString(target);

        targets.Add(targetString);
    }

    public void AddComponentType(
        Stack<TreeItem> currentPath, ReadOnlySpan<byte> type
    )
    {
        var componentName = currentPath.Reverse().Skip(3).Take(1).Single().PropertyName;

        if (componentName is not null)
        {
            var typeString = Encoding.UTF8.GetString(type);

            _components[$"#/components/schemas/{componentName}"] = typeString;
        }
    }

    public void AddDeprecatedType(Stack<TreeItem> currentPath)
    {
        var path = currentPath.Reverse().Collect();

        _deprecatedComponents.Add(path);
    }

    public Version? GetVersion() => _targetApiVersion;

    public bool HasKnownOneOfMapping(Stack<TreeItem> currentPath)
    {
        var path = currentPath.Reverse().Collect();

        if (_discriminatorData.TryGetValue(path, out var targets))
        {
            foreach (var target in targets)
            {
                if (!_components.ContainsKey(target))
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    public IEnumerable<KeyValuePair<string, string>> GetOneOfMapping(Stack<TreeItem> currentPath)
    {
        var path = currentPath.Reverse().Collect();

        if (_discriminatorData.TryGetValue(path, out var targets))
        {
            var mappings = targets.Select(x => new
            {
                discriminator = _components[x]!,
                reference = x,
                isDeprecated = _deprecatedComponents.Contains(x),
            }).ToList();

            foreach (var grouping in mappings.ToLookup(x => x.discriminator))
            {
                if (grouping.Count() > 1)
                {
                    foreach (var deprecated in grouping.Where(x => x.isDeprecated))
                    {
                        mappings.Remove(deprecated);
                    }
                }
            }

            foreach (var target in mappings)
            {
                yield return KeyValuePair.Create(target.discriminator, target.reference);
            }
        }
    }
}
