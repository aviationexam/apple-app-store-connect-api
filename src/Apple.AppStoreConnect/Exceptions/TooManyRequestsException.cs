using System;
using System.Collections.Generic;

namespace Apple.AppStoreConnect.Exceptions;

public class TooManyRequestsException : Exception
{
    public IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }

    public TooManyRequestsException(
        IReadOnlyDictionary<string, IEnumerable<string>> headers
    )
    {
        Headers = headers;
    }
}
