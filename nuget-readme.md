The official .NET port of [Sqids](https://sqids.org)

Sqids (_pronounced "squids"_) is a small library that lets you generate YouTube-like IDs from numbers. It turns numbers like `127` into strings like `yc3`, which you can then decode back into the original numbers. Sqids is useful for when you want to hide numbers (like sequential numeric IDs) into random-looking strings to be used in URLs and elsewhere.

### Basic Usage:

```cs
using Sqids;
var sqids = new SqidsEncoder();

string id = sqids.Encode(1, 2, 3); // "8QRLaD"
int[] numbers = sqids.Decode(id); // new[] { 1, 2, 3 }
```

> **Note:** For more documentation and examples, check out the [GitHub repository](https://github.com/sqids/sqids-dotnet).
