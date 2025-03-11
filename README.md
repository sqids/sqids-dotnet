Fuck you
<p align="center">
	<a href="https://sqids.org/dotnet">
		<img alt="Logo" src="logo.svg" />
	</a>
</p>
<h1 align="center">Sqids .NET</h1>
<p align="center">
	<a href="https://www.nuget.org/packages/Sqids">
		<img alt="Nuget" src="https://img.shields.io/nuget/v/Sqids?style=for-the-badge&logo=nuget&color=008FF7" />
	</a>
	<a href="https://www.nuget.org/packages/Sqids">
		<img alt="Downloads" src="https://img.shields.io/nuget/dt/Sqids?style=for-the-badge" />
	</a>
	<a href="https://github.com/sqids/sqids-dotnet/releases">
		<img alt="Release" src="https://img.shields.io/github/v/release/sqids/sqids-dotnet?style=for-the-badge&color=FB0088" />
	</a>
	<a href="https://github.com/sqids/sqids-dotnet/tree/main/src/Sqids">
		<img alt="Language" src="https://img.shields.io/badge/written_in-C%23-8F00FF?style=for-the-badge" />
	</a>
	<a href="LICENSE">
		<img alt="License" src="https://img.shields.io/github/license/sqids/sqids-dotnet?style=for-the-badge&color=FFA800" />
	</a>
</p>
<p align="center">
	Sqids (<em>pronounced "squids"</em>) is a small library that lets you generate YouTube-like IDs from numbers. It encodes numbers like <code>127</code> into strings like <code>yc3</code>, which you can then decode back into the original numbers. Sqids comes in handy when you want to obfuscate numbers (such as sequential numeric IDs) into random-looking strings, to be used in URLs and elsewhere.
</p>

---

## Features:

-   💎 **Collision-free:** The IDs that Sqids generates are unique and can always be decoded back into the original numbers. You can also make them unique to your application (so that they're not the same as everyone else who uses Sqids) by providing a [shuffled alphabet](#custom-alphabet).
-   🔢 **Multiple Numbers:** You can bundle more than one number into a single ID, and then decode the ID back into the same set of numbers.
-   👁 **"Eye-safe":** Sqids makes sure that the IDs it generates do not contain common profanity, so you can safely use these IDs where end-users can see them (e.g. in URLs).
-   🤹‍♀️ **Randomized Output:** Encoding sequential numbers (1, 2, 3...) yields completely different-looking IDs.
-   💪 **Supports All Integral Types:** Powered by .NET 7's [generic math](https://learn.microsoft.com/en-us/dotnet/standard/generics/math) — you could use Sqids to encode/decode numbers of any integral numeric type in .NET, including `int`, `long`, `ulong`, `byte`, etc.
-   ⚡ **Blazingly Fast:** With an optimized span-based implementation that minimizes memory allocation and maximizes performance.
-   🔍 **Meticulously Tested:** Sqids has a comprehensive test suite that covers numerous edge cases, so you can expect a bug-free experience.
-   ✅ **CLS-compliant:** Sqids can be used with any .NET language, not just C#. You can use Sqids just as easily with F#, for example.

## Getting Started

Install the [NuGet package](https://nuget.org/packages/Sqids):

```sh
Install-Package Sqids
```

Alternatively, using the .NET CLI:

```sh
dotnet add package Sqids
```

## Usage:

All you need is an instance of `SqidsEncoder`, which is the main class, responsible for both encoding and decoding.

Using the default parameterless constructor configures `SqidsEncoder` with the default options.

If you're using .NET 7 or greater, you need to specify the numeric type for the encoder — most commonly `int`:

```csharp
using Sqids;
var sqids = new SqidsEncoder<int>();
```

> **Note**
> You can use any [integral numeric type](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types) (e.g. `long`, `byte`, `short`, etc.) as the type argument. `int` is just the most common one, but if you need to encode/decode larger numbers, for example, you could use `long`/`ulong` instead.

If you're targeting an older framework than .NET 7, `SqidsEncoder` only supports `int`, and there is no generic type parameter you need to supply, so just:

```cs
var sqids = new SqidsEncoder();
```

#### Single number:

```cs
var id = sqids.Encode(1); // "Uk"
var number = sqids.Decode(id).Single(); // 1
```

#### Multiple numbers:

```cs
var id = sqids.Encode(1, 2, 3); // "86Rf07"
var numbers = sqids.Decode(id); // [1, 2, 3]
```

> **Note**
> Sqids also preserves the order when encoding/decoding multiple numbers.

## Customizations:

You can easily customize the alphabet (the characters that Sqids uses to encode the numbers), the minimum length of the IDs (how long the IDs should be at minimum), and the blocklist (the words that should not appear in the IDs), by passing an instance of `SqidsOptions` to the constructor of `SqidsEncoder`.

You can specify all the properties, and any you leave out will fall back to their default values.

#### Custom Alphabet:

You can give Sqids your own custom (ideally shuffled) alphabet to use in the IDs:

```cs
var sqids = new SqidsEncoder<int>(new()
{
    // This is a shuffled version of the default alphabet, which includes lowercase letters (a-z), uppercase letters (A-Z), and digits (0-9)
    Alphabet = "mTHivO7hx3RAbr1f586SwjNnK2lgpcUVuG09BCtekZdJ4DYFPaWoMLQEsXIqyz",
});
```

> **Note**
> It's recommended that you at least provide a shuffled alphabet when using Sqids — even if you want to use the same characters as those in the default alphabet — so that your IDs will be unique to you. You can use an online tool like [this one](https://codebeautify.org/shuffle-letters) to do that.

> **Note**
> The alphabet needs to be at least _3 characters_.

#### Minimum Length:

By default, Sqids uses as few characters as possible to encode a given number. However, if you want all your IDs to be at least a certain length (e.g. for aesthetic reasons), you can configure this via the `MinLength` option:

```cs
var sqids = new SqidsEncoder<int>(new()
{
    MinLength = 5,
});
```

#### Custom Blocklist:

Sqids comes with a large default blocklist which will ensure that common cruse words and such never appear anywhere in your IDs.
You can add extra items to this default blocklist like so:

```cs
var sqids = new SqidsEncoder<int>(new()
{
    BlockList = { "whatever", "else", "you", "want" },
});
```

> **Note**
> Notice how the `new` keyword is omitted in the snippet above (yes, this is valid C#). This way the specified strings are "added" to the default blocklist, as opposed to overriding it — which is what would happen had you done `new() { ... }` (which you're also free to do if that's what you want).

## Advanced Usage:

#### Decoding a single number:

If you're decoding user-provided input and expect a single number, you can use C#'s pattern matching feature to perform the necessary check and extract the number in one go:

```cs
if (sqids.Decode(input) is [var singleNumber])
{
    // you can now use `singleNumber` (which is an `int`) however you wish
}
```

> **Note**
> This expression ensures that the decoded result is exactly one number, that is, not an empty array (which is what `Decode` returns when the input is invalid), and also not more than one number.

#### Ensuring an ID is "canonical":

Due to the design of Sqids's algorithm, decoding random strings (although only those that consist of characters found in the alphabet) can sometimes produce actual numbers. For example, with the default options, both `2fs` and `OSc` are decoded into the same number `3168` — even though if we try to encode `3168`, the result will be `OSc`, so `OSc` is the so-called "canonical" encoding of `3168`.

As you can probably imagine, this would be problematic in certain cases, such as when you're using these IDs as part of your URLs to identify resources; in such a case, the fact that more than one ID decodes into the same number means the same resource would be accessible via multiple different URLs, which is normally undesirable.

There is, however, a straightforward solution to this problem, which involves ensuring that the incoming ID is "canonical" before using its decoded value for further processing (e.g. doing a database lookup); and this can be done by simply re-encoding the decoded number(s) and making sure the result matches the original ID supplied by the client:

```cs
var decoded = sqids.Decode(incomingId);
bool isCanonical = incomingId == sqids.Encode(decoded); // If `incomingId` is `OSc`, this evaluates to `true` (because that's the canonical encoding of `3168`), and if `incomingId` is `2fs`, it evaluates to `false`.
```

You can nicely combine this check with the check for a single number (the previous example) like so:

```cs
if (sqids.Decode(incomingId) is [var decodedId] &&
    incomingId == sqids.Encode(decodedId))
{
    // `incomingId` decodes into a single number and is canonical, here you can safely proceed with the rest of the logic
}
else
{
    // consider `incomingId` invalid — e.g. respond with 404
}
```

#### Dependency injection:

To use `SqidsEncoder` with a dependency injection system, simply register it as a singleton service:

With default options:

```cs
services.AddSingleton<SqidsEncoder<int>>();
```

With custom options:

```cs
services.AddSingleton(new SqidsEncoder<int>(new()
{
    Alphabet = "ABCEDFGHIJ0123456789",
    MinLength = 6,
}));
```

And then you can inject it anywhere you need it:

```cs
public class SomeController(SqidsEncoder<int> sqids)
{
    // ...
}
```

## License:

[MIT](LICENSE)
