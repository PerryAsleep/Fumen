# StepGraphs

`StepGraphs` represent all the possible positions the body can be in and all the moves it can make to travel between those positions for a given [ChartType](ChartType.md).

Positions are defined by [PadData](PadData.md). Moves are defined by [StepTypes](StepTypes.md).

## StepGraph Files

Step graphs are defined in files named after their [ChartType](ChartType.md) with the `fsg` extension. For example `dance-single.fsg`. These files can be generated through the [PadDataGenerator](../../PadDataGenerator/docs/Readme.md) application.