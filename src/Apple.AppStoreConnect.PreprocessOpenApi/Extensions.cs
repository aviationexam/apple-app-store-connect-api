namespace Apple.AppStoreConnect.PreprocessOpenApi;

public static class Extensions
{
    public static string Collect(
        this IEnumerable<TreeItem> stack
    ) => string.Join('.', stack.Select(x => x.PropertyName));
}
