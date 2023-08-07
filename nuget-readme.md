The official .NET port of [Sqids](https://sqids.org)

Sqids (pronounced "squids") is a small library that lets you generate YouTube-looking IDs from numbers. It's good for link shortening, fast & URL-safe ID generation and decoding back into numbers for quicker database lookups.

### Basic Usage:

```cs
using Sqids;
var sqids = new SqidsEncoder();

string id = sqids.Encode(1, 2, 3); // "8QRLaD"
int[] numbers = sqids.Decode(id); // new[] { 1, 2, 3 }
```

> **Note:** For more documentation and examples, check out the [GitHub repository](https://github.com/sqids/sqids-dotnet).
