[![Build Status](https://github.com/aviationexam/apple-app-store-connect-api/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/aviationexam/apple-app-store-connect-api/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/Aviationexam.Apple.AppStoreConnect.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/Aviationexam.Apple.AppStoreConnect/)
[![MyGet](https://img.shields.io/myget/apple-app-store-connect/vpre/Aviationexam.Apple.AppStoreConnect?label=MyGet)](https://www.myget.org/feed/apple-app-store-connect/package/nuget/Aviationexam.Apple.AppStoreConnect)

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

serviceCollection.AddAppleAppStoreConnect(
  optionsBuilder => optionsBuilder.Configure()
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
