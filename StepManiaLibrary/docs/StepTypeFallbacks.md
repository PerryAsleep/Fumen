# StepType Fallbacks

[StepType](StepTypes.md) fallbacks define a list of `StepTypes` that are acceptable to use as replacements for another `StepType` when generating a [PerformedChart](PerformedChart.md). These replacements are only used if no path could be found with the original `StepType`. For example, if generating a `PerformedChart` containing stretch moves using [PadData](PadData.md) that doesn't support stretch (e.g. `dance-single`), then the stetch moves will need to fall back to other moves. Individual `StepType` fallback definitions are ordered lists with the first replacement being most preferable.

## Example

```json
// A NewArrow step can fallback to a SameArrow step.
"NewArrow": [
	"NewArrow",
	"SameArrow",
],

// Stretch crossover in back can fallback to normal crossovers, then a NewArrow step, then a SameArrow step.
"CrossoverBehindStretch": [
	"CrossoverBehindStretch",
	"CrossoverFrontStretch",
	"CrossoverBehind",
	"CrossoverFront",
	"NewArrow",
	"SameArrow",
],
```

## Omitting Arrows

When falling back it is possible to completely omit arrows. For example if chart requires holding four arrows at once but the `PadData` only supports three arrows total (e.g. `smx-beginner` or `dance-threepanel`) then there is no way to keep all four holds. The worst fallback for each `StepType` is a blank step which completely removes the arrows, but this is avoided unless absolutely necessary.

## Configuration

The default set of fallbacks is defined in `default-steptype-fallbacks.json` and it is not recommended to edit this file.

## Cost Determination

When needing to choose between paths which both contain fallbacks their fallback costs are compared and the path with the lowest cost is chosen. The cost to perform a fallback scales with the number of fallbacks available for the given `StepType` as it is generally more preferable to perform a gradual fallback across many options.

Fallbacks cost is determined as follows:
- If the fallback is the first fallback the cost is `0.0`.
- Otherwise the cost is `(index / (number_of_fallbacks - 1))`.

In addition, any any dropped arrows as a result of falling back (e.g. falling back from `BracketHeelNewToeNew` to `NewArrow`) accrue an additional cost of `100.0`.

Using the example data above:
 - Falling back from `CrossoverBehindStretch` to `CrossoverFront` costs `0.6` because `3 / (6 - 1) = 0.6`.
 - Falling back from `NewArrow` to `SameArrow` costs `1.0` because `1 / (2 - 1) = 1.0`.
 - Fallng back from `NewArrow` to `NewArrow` costs `0.0` because it is the first fallback.
