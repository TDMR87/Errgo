# Errgo

Go-style errors-as-values in C#. Instead of throwing exceptions, you return a tuple of (`result`, `Error`) and check the error explicitly at the call-site.

## Install

```bash
dotnet add package Errgo
```

## Quick start

```csharp
using Errgo;

var (user, err) = GetUserById("1234");
if (err) return (null, err);
else return (user, Error.None);

```


`Error` is a struct, so it never allocates. `Error.None` signals success. `default(Error)` is still an error. If it's not `Error.None`, it's a failure.

## Features

- **Value type** - no heap allocations, equality compared by value
- **Sentinel errors** - `Error.Sentinel(...)` for creating known errors to match against
- **Composition** - `Error.Join(...)` combines multiple errors into a chain of inner errors
- **Error stack** - Print the whole error stack of an error and it's accumulated inner errors, source file and file number included
- **Pattern matching** - `Is(Error)` and `As(Error, out Error)` to branch on specific errors
- **Companion analyzer** - [Errgo.Analyzer](https://www.nuget.org/packages/Errgo.Analyzer/) enforces the errors-as-values style in your IDE

## Requirements

- .NET Standard 2.1

## License

MIT — see the [repository](https://github.com/TDMR87/Errgo) for the full source and documentation.