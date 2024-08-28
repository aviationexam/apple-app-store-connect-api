using System.Text;

namespace Apple.AppStoreConnect.PreprocessOpenApi;

public sealed class CollectedMetadata
{
    private readonly IDictionary<string, ICollection<string>> _discriminatorData = new Dictionary<string, ICollection<string>>();

    public void AddOneOf(
        Stack<TreeItem> stack, ReadOnlySpan<byte> target
    )
    {
        var path = stack.Skip(1).Collect();

        if (!_discriminatorData.TryGetValue(path, out var targets))
        {
            targets = new List<string>();
            _discriminatorData.Add(path, targets);
        }

        targets.Add(Encoding.UTF8.GetString(target));
    }
}
