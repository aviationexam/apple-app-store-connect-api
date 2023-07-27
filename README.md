[![Build Status](https://github.com/aviationexam/apple-app-store-connect-api/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/aviationexam/apple-app-store-connect-api/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/Aviationexam.Apple.AppStoreConnect.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/Aviationexam.Apple.AppStoreConnect/)
[![MyGet](https://img.shields.io/myget/apple-app-store-connect/vpre/Aviationexam.Apple.AppStoreConnect?label=MyGet)](https://www.myget.org/feed/apple-app-store-connect/package/nuget/Aviationexam.Apple.AppStoreConnect)
[![feedz.io](https://img.shields.io/badge/endpoint.svg?url=https%3A%2F%2Ff.feedz.io%2Faviationexam%2Fapple-app-store-connect-api%2Fshield%2FAviationexam.Apple.AppStoreConnect%2Flatest&label=Aviationexam.Apple.AppStoreConnect)](https://f.feedz.io/aviationexam/apple-app-store-connect-api/packages/Aviationexam.Apple.AppStoreConnect/latest/download)

# Apple AppStoreConnect

## Install
```xml
<ItemGroup>
    <PackageReference Include="Apple.AppStoreConnect" Version="" />
    <PackageReference Include="Apple.AppStoreConnect.DependencyInjection" Version="" />
</ItemGroup>
```

## How to configure library

Add library to the dependency container

```cs
using Apple.AppStoreConnect.DependencyInjection;

IServiceCollection serviceCollection;

// you may need to add these dependencies
serviceCollection.AddMemoryCache();
using Microsoft.Extensions.Internal;
serviceCollection.TryAddSingleton<ISystemClock, SystemClock>();

// configure AppStoreConnect services
serviceCollection.AddAppleAppStoreConnect(optionsBuilder => optionsBuilder
  .Configure()
  .ValidateDataAnnotations()
);
// OR
serviceCollection.AddAppleAppStoreConnect(optionsBuilder => optionsBuilder
  .Bind(builder.Configuration.GetSection(MyConfigOptions.MyConfig))
  .ValidateDataAnnotations()
);
// OR
serviceCollection.AddAppleAppStoreConnect(
  optionsBuilder => optionsBuilder.Configure(),
  new Dictionary<Type, Action<IHttpClientBuilder>> {
    [typeof(IAppStoreConnectClient)] = httpClientBuilder => {
      // here you can configure all managed http clients
    },

    [typeof(IAgeRatingDeclarationsClient)] = httpClientBuilder => {
      // or you can configure one specific http client
    },
  }
);
```

## How to use library

There are many clients generated from `openapi.json`.
This library make some minor changes to the document, mainly to improve quality of generated client or to fix some issues.

Generated clients follows naming convention: `I{tags.first}Client`, you can simply access it using dependency injection or from your service provider.
```cs

```
