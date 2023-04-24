using Microsoft.IdentityModel.JsonWebTokens;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Apple.AppStoreConnect;

public sealed class AppleAuthenticationOptions
{
    [Required]
    public string KeyId { get; set; } = null!;

    [Required]
    public string IssuerId { get; set; } = null!;

    [Required]
    public string TokenAudience { get; set; } = null!;

    /// <summary>
    /// Gets or sets an optional delegate to get the client's private key which is passed
    /// the value of the <see cref="KeyId"/> property and the <see cref="CancellationToken"/>
    /// associated with the current HTTP request.
    /// </summary>
    /// <remarks>
    /// The private key should be in PKCS #8 (<c>.p8</c>) format.
    /// </remarks>
    [Required]
    public Func<string, CancellationToken, Task<ReadOnlyMemory<char>>>? PrivateKey { get; set; }

    /// <summary>
    /// Gets or sets the optional <see cref="JsonWebTokenHandler"/> to use.
    /// </summary>
    public JsonWebTokenHandler SecurityTokenHandler { get; set; } = default!;

    [Required]
    public TimeSpan JwtExpiresAfter { get; set; }
}
