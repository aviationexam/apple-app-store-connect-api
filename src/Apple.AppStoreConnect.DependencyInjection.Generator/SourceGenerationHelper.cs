namespace Apple.AppStoreConnect.DependencyInjection.Generator;

public static class SourceGenerationHelper
{
    public const string AppStoreConnectDependencyInjectionAttributeFullname = "Apple.AppStoreConnect.AppStoreConnectDependencyInjectionAttribute";

    public const string AppStoreConnectDependencyInjectionAttribute = """
namespace Apple.AppStoreConnect;

[System.AttributeUsage(System.AttributeTargets.Class)]
public class AppStoreConnectDependencyInjectionAttribute : System.Attribute
{
}
""";
}
