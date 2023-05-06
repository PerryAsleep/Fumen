# PadDataGenerator

`PadDataGenerator` is an application for generating [PadData](../../StepManiaLibrary/docs/PadData.md) files and [StepGraph](../../StepManiaLibrary/docs/StepGraphs.md) files for a [ChartType](../../StepManiaLibrary/docs/ChartType.md) from simple input.

## Usage

Double-click `PadDataGenerator.exe`

## Configuration

`PadDataGenerator` can be configured via the `input.json` file in the application's install directory. The `PadDataInput` object is a dictionary of `ChartType` to Input objects.

### Input Objects

- **Positions**: Array type. The X and Y positions of all the arrows which make up the `PadData`. It is expected this array is ordered matching the order the arrows are displayed during the game.
- **MaxXSeparationBeforeStretch**: Optional. Integer type. Defualt `2`. The maximum separation in X before considering two arrows to result in a stretch position.
- **MaxYSeparationBeforeStretch**: Optional. Integer type. Defualt `2`. The maximum separation in Y before considering two arrows to result in a stretch position.
- **MaxXSeparationCrossoverBeforeStretch**: Optional. Integer type. Defualt `1`. The maximum separation in X before considering two arrows to result in a stretch position when crossed over.
- **MaxYSeparationCrossoverBeforeStretch**: Optional. Integer type. Defualt `2`. The maximum separation in Y before considering two arrows to result in a stretch position when crossed over.
- **MaxXSeparationBracket**: Optional. Integer type. Defualt `1`. The maximum separation in X to consider two arrows to be bracketable together.
- **MaxYSeparationBracket**: Optional. Integer type. Defualt `1`. The maximum separation in Y to consider two arrows to be bracketable together.
- **YTravelDistanceCompensation**: Optional. Double type. Defualt `0.5`. The `YTravelDistanceCompensation` value to set in the resulting `PadData`.
- **GenerateStepGraph**: Optional. Boolean type. Defualt `true`. Whether or not to generate a `StepGraph` file for this `ChartType`.

### Example `input.json`

```json
{
	"PadDataInput":
	{
		"dance-single":
		{
			"Positions": [
				{"X": 0, "Y": 1},
				{"X": 1, "Y": 2},
				{"X": 1, "Y": 0},
				{"X": 2, "Y": 1},
			],
		},
		"dance-double":
		{
			"Positions": [
				{"X": 0, "Y": 1},
				{"X": 1, "Y": 2},
				{"X": 1, "Y": 0},
				{"X": 2, "Y": 1},
				{"X": 3, "Y": 1},
				{"X": 4, "Y": 2},
				{"X": 4, "Y": 0},
				{"X": 5, "Y": 1},
			],
		},
		"dance-solo":
		{
			"Positions": [
				{"X": 0, "Y": 1},
				{"X": 0, "Y": 0},
				{"X": 1, "Y": 2},
				{"X": 1, "Y": 0},
				{"X": 2, "Y": 0},
				{"X": 2, "Y": 1},
			],
		},
		"dance-threepanel":
		{
			"Positions": [
				{"X": 0, "Y": 0},
				{"X": 1, "Y": 2},
				{"X": 2, "Y": 0},
			],
		},
		"pump-single":
		{
			"Positions": [
				{"X": 0, "Y": 2},
				{"X": 0, "Y": 0},
				{"X": 1, "Y": 1},
				{"X": 2, "Y": 0},
				{"X": 2, "Y": 2},
			],
		},
		"pump-halfdouble":
		{
			"Positions": [
				{"X": 0, "Y": 1},
				{"X": 1, "Y": 0},
				{"X": 1, "Y": 2},
				{"X": 2, "Y": 2},
				{"X": 2, "Y": 0},
				{"X": 3, "Y": 1},
			],
		},
		"pump-double":
		{
			"Positions": [
				{"X": 0, "Y": 2},
				{"X": 0, "Y": 0},
				{"X": 1, "Y": 1},
				{"X": 2, "Y": 0},
				{"X": 2, "Y": 2},
				{"X": 3, "Y": 2},
				{"X": 3, "Y": 0},
				{"X": 4, "Y": 1},
				{"X": 5, "Y": 0},
				{"X": 5, "Y": 2},
			],
		},
		"smx-beginner":
		{
			"Positions": [
				{"X": 0, "Y": 0},
				{"X": 1, "Y": 0},
				{"X": 2, "Y": 0},
			],
		},
		"smx-single":
		{
			"Positions": [
				{"X": 0, "Y": 1},
				{"X": 1, "Y": 2},
				{"X": 1, "Y": 1},
				{"X": 1, "Y": 0},
				{"X": 2, "Y": 1},
			],
		},
		"smx-dual":
		{
			"Positions": [
				{"X": 0, "Y": 0},
				{"X": 1, "Y": 0},
				{"X": 2, "Y": 0},
				{"X": 3, "Y": 0},
				{"X": 4, "Y": 0},
				{"X": 5, "Y": 0},
			],
		},
		"smx-full":
		{
			"Positions": [
				{"X": 0, "Y": 1},
				{"X": 1, "Y": 2},
				{"X": 1, "Y": 1},
				{"X": 1, "Y": 0},
				{"X": 2, "Y": 1},
				{"X": 3, "Y": 1},
				{"X": 4, "Y": 2},
				{"X": 4, "Y": 1},
				{"X": 4, "Y": 0},
				{"X": 5, "Y": 1},
			],
		},
	}
}
```