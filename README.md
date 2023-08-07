# [Sqids .NET](https://sqids.org/dotnet)

Sqids (pronounced "squids") is a small library that lets you generate YouTube-looking IDs from numbers. It's good for link shortening, fast & URL-safe ID generation and decoding back into numbers for quicker database lookups.

# Getting Started

Install the [NuGet package](https://nuget.org/packages/Sqids):

```sh
Install-Package Sqids
```

Alternatively, using the .NET CLI:

```sh
dotnet add package Sqids
```

# Usage:

All you need is an instance of `SqidsEncoder`, which is the main class, responsible for both encoding and decoding.

Using the default parameterless constructor configures `SqidsEncoder` with the default options:

```csharp
using Sqids;
var sqids = new SqidsEncoder();
```

### Single number:

```cs
string id = sqids.Encode(1); // "UfB"
int number = sqids.Decode(id).Single(); // 123
```

### Multiple numbers:

```cs
string id = sqids.Encode(1, 2, 3); // "8QRLaD"
int[] numbers = sqids.Decode(id); // new[] { 1, 2, 3 }
```

> **Note**
> Sqids also preserves the order when encoding/decoding multiple numbers.

# Customizations:

You can easily customize the alphabet (the characters that Sqids uses to encode the numbers), the minimum length of the IDs (how long the IDs should be at minimum), and the blocklist (the words that should not appear in the IDs), by passing an instance of `SqidsOptions` to the constructor of `SqidsEncoder`.

You can specify all the properties, and any you leave out will fall back to their default values.

### Custom Alphabet:

You can give Sqids your own custom (ideally shuffled) alphabet to use in the IDs:

```cs
var sqids = new SqidsEncoder(new()
{
    // This is a shuffled version of the default alphabet, which includes lowercase letters (a-z), uppercase letters (A-Z), and digits (0-9)
    Alphabet = "mTHivO7hx3RAbr1f586SwjNnK2lgpcUVuG09BCtekZdJ4DYFPaWoMLQEsXIqyz",
});
```

> **Note**
> It's recommended that you at least provide a shuffled alphabet when using Sqids — even if you want to use the same characters as those the default alphabet — so that your IDs will be unique to you.

### Minimum Length:

By default, Sqids uses as few characters as possible to encode a given number. However, if you want all your IDs to be at least a certain length (e.g. for aesthetic reasons), you can configure this via the `MinLength` option:

```cs
var sqids = new SqidsEncoder(new()
{
    MinLength = 5,
});
```

### Custom Blocklist:

Sqids comes with a large default blocklist which will ensure that common cruse words and such never appear anywhere in your IDs.
You can add extra items to this default blocklist like so:

```cs
var sqids = new SqidsEncoder(new()
{
    BlockList = { "whatever", "else", "you", "want" },
});
```

> **Note**
> Notice how the `new` keyword is omitted in the snippet above (yes, this is valid C#). This way the specified strings are "added" to the default blocklist, as opposed to overriding it — which is what would happen had you done `new() { ... }` (which you're also free to do if that's what you want).

# Advanced Usage:

### Decoding a single number:

If you're decoding user-provided input and expect a single number, you can use C# pattern matching to do the necessary check and extract the number in one go:

```cs
if (sqids.Decode(input) is [var singleNumber])
{
    // you can now use `singleNumber` (which is an `int`) however you wish
}
```

> **Note**
> This expression ensures that the decoded result is exactly one number, that is, not an empty array (which is what `Decode` returns when the input is invalid), and also not more than one number.

### Ensuring an ID is "canonical":

Due to the design of Sqids's algorithm, decoding random IDs can sometimes produce the same numbers. For example, with the default options, both `2fs` and `OSc` are decoded into the number `3168`. This can be problematic in certain cases, such as when you're using these IDs as part of your URLs to identify resources; this way, the fact that more than one ID decodes into the same number means the same resource would be accessible with two different URLs, which is often undesirable.

The best way to mitigate this is to check if an ID is "canonical" before using its decoded value to do a database lookup, for example; and this can be done by simply re-encoding the decoded number(s) and checking if the result matches the incoming ID:

```cs
int[] numbers = sqids.Decode(input);
bool isCanonical = input == sqids.Encode(numbers); // If `input` is `OSc`, this evaluates to `true` (because that's the canonical encoding of `3168`), and if `input` is `2fs`, it evaluates to `false`.
```

You can combine this check with the check for a single number (the previous example) like so:

```cs
if (sqids.Decode(input) is [var id] &&
    input == sqids.Encode(id))
{
    // `input` decodes into a single number and is canonical, now you can safely use it
}
```

### Dependency injection:

To use `SqidsEncoder` with a dependency injection system, simply register it as a singleton service:

With default options:

```cs
services.AddSingleton<SqidsEncoder>();
```

With custom options:

```cs
services.AddSingleton(new SqidsEncoder(new()
{
    Alphabet = "ABCEDFGHIJ0123456789",
    MinLength = 6,
}));
```

And then you can inject it anywhere you need it:

```cs
public class SomeController
{
    private readonly SqidsEncoder _sqids;
    public SomeController(SqidsEncoder sqids)
    {
        _sqids = sqids;
    }
}
```

## License

[MIT](LICENSE)
