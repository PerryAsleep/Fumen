# ExpressedChart

`ExpressedCharts` represent a series of kinds of steps through a chart, described primarily through the [StepTypes](StepTypes.md). `ExpressedCharts` are agnostic to the [ChartType](ChartType.md) of a source chart. An `ExpressedChart` can be used as input to a [PerformedChart](PerformedChart.md) in order to generate a new chart of a different `ChartType`.

`ExpressedCharts` are created by parsing a chart for a given `ChartType` and using that `ChartType`'s [PadData](PadData.md) and [StepGraph](StepGraph.md) to search every possible set of moves which would satisfy the chart. Costs are assigned to each move based on how likely they are to be the intended way to execute the chart, and the path with the lowest cost is chosen.

## ExpressedChart Configuration

`ExpressedChart` behavior can be configured through json objects described below.

### Example

```json
{
	"DefaultBracketParsingMethod": "Balanced",
	"BracketParsingDetermination": "ChooseMethodDynamically",
	"MinLevelForBrackets": 9,
	"UseAggressiveBracketsWhenMoreSimultaneousNotesThanCanBeCoveredWithoutBrackets": true,
	// Interpret charts with three brackets per minute or more as charts which should
	// aggressively interpret bracketable jumps as brackets.
	"BalancedBracketsPerMinuteForAggressiveBrackets": 3.0,
	// Interpret charts with one bracket every minute and 45 seconds or less as charts
	// which should not have brackets. Many non-technical charts have jumps which patterns
	// which can reasonably be done by bracketing. With balanced parsing, these charts
	// would produce brackets. This threshold helps keeps non-technical charts bracket-free.
	"BalancedBracketsPerMinuteForNoBrackets": 0.571,
}
```

### Configuration

- **DefaultBracketParsingMethod**: String type. The default method to use for parsing steps which could be jumps or brackets. The default method is used when the `BracketParsingDetermination` is set to `"UseDefaultMethod"`. Valid `BracketParsingMethod` values are:
	- `"Aggressive"`: The application will try to interpret patterns aggressively as brackets instead of jumps. It is difficult to exhaust all the conditions under which brackets will be preferred as it relates to the costs of the surrounding patterns. For a more detailed description of how the `"Aggressive"` behavior works, please see [ExpressedChart.cs](https://github.com/PerryAsleep/Fumen/blob/master/StepManiaChartGenerator/ExpressedChart.cs).
	- `"Balanced"`: The application will attempt to take a balanced approach to interpreting brackets and jumps. Generally, this errs on choosing brackets in ambiguous situations. It is difficult to exhaust all the conditions under which brackets will be preferred as it relates to the costs of the surrounding patterns. For a more detailed description of how the `"Balanced"` behavior works, please see [ExpressedChart.cs](https://github.com/PerryAsleep/Fumen/blob/master/StepManiaChartGenerator/ExpressedChart.cs).
	- `"NoBrackets"`: Jumps will be preferred to brackets. Steps will still be expressed as brackets if there are more than two simultaneous notes.
- **BracketParsingDetermination**: String type. How the application should determine which `BracketParsingMethod` to use. Valid `BracketParsingDetermination` values are:
	- `"ChooseMethodDynamically"`: The application will use the properties on this `ExpressedChartConfig` object to determine which `BracketParsingMethod` to use for the chart, falling back to the `"Balanced"` method.
	- `"UseDefaultMethod"`: The `DefaultBracketParsingMethod` will be used.
- **MinLevelForBrackets**: Number (integer) type. When parsing using `"ChooseMethodDynamically"`, charts with a difficulty rating under this level will use the `"NoBrackets"` `BracketParsingMethod`.
- **UseAggressiveBracketsWhenMoreSimultaneousNotesThanCanBeCoveredWithoutBrackets**: Boolean type. When parsing using `"ChooseMethodDynamically"` and the chart is above the `MinLevelForBrackets`, and this value is `true`, then use the `"Aggressive"` `BracketParsingMethod` if there exist more simultaneous steps in the chart than can be covered without brackets. In other words, if there are patterns that cannot possibly be performed without bracketing, then use the `"Aggressive"` method for the chart.
- **BalancedBracketsPerMinuteForAggressiveBrackets**: Number (double) type. When parsing using `"ChooseMethodDynamically"` and the above parameters have still not determined which `BracketParsingMethod` to use, then parse the chart using the `"Balanced"` `BracketParsingMethod` and determine the number of brackets per minute. If the brackets per minute is above this value, then switch to the `"Aggressive"` method for the chart.
- **BalancedBracketsPerMinuteForNoBrackets**: Number (double) type. When parsing using `"ChooseMethodDynamically"` and the above parameters have still not determined which `BracketParsingMethod` to use, then parse the chart using the `"Balanced"` `BracketParsingMethod` and determine the number of brackets per minute. If the brackets per minute is below this value, then switch to the `"NoBrackets"` method for the chart.