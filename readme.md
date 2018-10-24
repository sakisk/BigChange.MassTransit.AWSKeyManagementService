# BigChange.MassTransit.AWSKeyManagementService

Encrypt your MassTransit messages with AWS Key Management Service (KMS)!

## Get started

### Install the NuGet Package

You can install the package using the standard dotnet CLI:

```bash
dotnet add package BigChange.MassTransit.AwsKeyManagementService
```

or by using the package manager within Visual Studio:

```powershell
PM> Install-Package BigChange.MassTransit.AwsKeyManagementService
```

## Setting up the bus

When "configuring the bus" you can call one of the following extension methods on either the `IBusFactoryConfigurator` or `IReceiveEndpointConfigurator`

### Configure with a Key Id

You can configure the bus to use a given Customer Master Key (CMK), this will be used to generate and encrypt the data encryption key that will be used for encrypting and decrypting the messages.

To specify a CMK, use its key ID, Amazon Resource Name (ARN), alias name, or alias ARN. When using an alias name, prefix it with "alias/". To specify a CMK in a different AWS account, you must use the key ARN or alias ARN.

```csharp
configurator.UseAwsKeyManagementServiceSerializer("alias/masstransit")
```

### Configure with a Key Id and an AWS Region

You can specify which region to use for fetching CMK:

```csharp
configurator.UseAwsKeyManagementServiceSerializer(RegionEndpoint.EUWest1, "alias/masstransit")

```

### Configure with a Key Id, an AWS Region and cache options to use `MemoryDistributedCache` for caching CMK  

You can specify `MemoryDistributedCacheOptions` to configure options for caching CMK in memory:

```csharp
configurator.UseAwsKeyManagementServiceSerializerWithMemoryCache(RegionEndpoint.EUWest1, "alias/masstransit", Options.Create(new MemoryDistributedCacheOptions()));

```

### Configure with a Key Id and a custom encryption context builder

You can customize how the library builds up the encryption context that will be used for encrypting and decrypting message data, the default implementation uses an empty encryption context. Note that all data used in the encryption context will logged if CloudTrail logging is turned on.

```csharp
public class CustomEncryptionContextBuilder : IEncryptionContextBuilder
{
    private static readonly Dictionary<string, string> MyEncryptionContext
        = new Dictionary<string, string>()
        {
            { "SomeKey", "SomeData" }
        };

    public Dictionary<string, string> BuildEncryptionContext(SendContext context)
        => MyEncryptionContext;

    public Dictionary<string, string> BuildEncryptionContext(ReceiveContext receiveContext)
        => MyEncryptionContext;
}

configurator.UseAwsKeyManagementServiceSerializer(new CustomEncryptionContextBuilder(), "alias/masstransit")
```

### Configure with a Key Id and a custom `IAmazonKeyManagementService` instance

You can configure the bus with a customized version of the `IAmazonKeyManagementService`, this is useful if you want to pass a custom configuration in to `AmazonKeyManagementServiceClient` or want the ability to mock out the calls to `IAmazonKeyManagementService`:

```csharp
var config = new AmazonKeyManagementServiceConfig();
var client = new AmazonKeyManagementServiceClient(config);

configurator.UseAwsKeyManagementServiceSerializer(client, "alias/masstransit")

```

## Contribute

1. Fork
1. Hack!
1. Pull Request