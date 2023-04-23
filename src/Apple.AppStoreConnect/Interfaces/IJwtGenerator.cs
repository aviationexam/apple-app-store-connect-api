using System.Threading;
using System.Threading.Tasks;

namespace Apple.AppStoreConnect.Interfaces;

public interface IJwtGenerator
{
    /// <summary>
    /// Generates a JWT token for Apple App Store Connect as an asynchronous operation.
    /// </summary>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation
    /// to generate a JWT token for Apple App Store Connect.
    /// </returns>
    Task<string> GenerateJwtTokenAsync(
        CancellationToken cancellationToken = default
    );
}
