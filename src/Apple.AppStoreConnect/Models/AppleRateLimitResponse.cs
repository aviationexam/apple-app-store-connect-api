namespace Apple.AppStoreConnect.Models;

public record AppleRateLimitResponse(
    int UserHourLimit,
    int UserHourRemaining
)
{
    public AppleRateLimitResponse() : this(0, 0)
    {
    }
}
