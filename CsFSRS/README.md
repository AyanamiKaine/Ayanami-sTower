# FsrsSharp

`FsrsSharp` is a C# port of the FSRS spaced repetition scheduler, based on the `py-fsrs` 5.1.3 implementation.

The goal is API and data-model compatibility with the Python library, not a C#-specific redesign. Card and review log objects can be converted to dictionaries for straightforward JSON serialization and interchange with Python tooling.

## Install

```bash
dotnet add package FsrsSharp
```

## Usage

```csharp
using FsrsSharp;

var scheduler = new Scheduler(enableFuzzing: false);
var card = new Card();

var result = scheduler.ReviewCard(card, Rating.Good, DateTimeOffset.UtcNow);

Card updatedCard = result.UpdatedCard;
ReviewLog log = result.Log;
```

## Notes

- Review timestamps must be in UTC.
- Large behavioral differences from `py-fsrs` should be treated as bugs.
