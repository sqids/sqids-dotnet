# [Sqids .NET](https://sqids.org/dot-net)

Sqids (pronounced "squids") is a small library that lets you generate YouTube-looking IDs from numbers. It's good for link shortening, fast & URL-safe ID generation and decoding back into numbers for quicker database lookups.

## Getting started

Install the [NuGet package](https://nuget.org/packages/Sqids):

```sh
Install-Package Sqids
```

Alternatively, using the .NET CLI:

```sh
dotnet add package Sqids
```

Basic usage:

```cs
using Sqids;

var sqids = new SqidsEncoder();
string id = sqids.Encode(123); // id = 'UfB'
int number = sqids.Decode(id).Single(); // number = '123'
```

## Examples

DI usage:

```cs
services.AddSingleton(new SqidsEncoder(new()
{
    Alphabet = "djpo9831",
}));
```

## License

[MIT](LICENSE)
