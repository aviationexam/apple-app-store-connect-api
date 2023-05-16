namespace Apple.AppStoreConnect.Models;

public enum ETooManyRequestParseLimit : byte
{
    Success,
    MissingHeader,
    TooManyHttpHeaders,
    UnexpectedHeaderProperty,
    HeaderPropertyValueIsNotANumber,
    HeaderContainsUnknownProperty,
}
