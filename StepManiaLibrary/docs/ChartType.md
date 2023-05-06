# ChartType

`ChartTypes` describe the kind of chart being played. In StepMania this is described as a [StepsType](https://github.com/stepmania/stepmania/blob/6a645b4710dd6a89a5f22a2d849e86a98af5c9a3/src/GameManager.cpp#L47). The supported `ChartTypes` are listed below. They match the StepMania `StepsTypes` with the addition of [StepManiaX](https://stepmaniax.com/) types.

The library often uses StepMania `StepsTypes` interchangeably with its `ChartType`, converting the `-` character from `StepsType`, which is an invalid character in C# enums, into `_`. Serialized data often uses the `StepsTypes` naming scheme with `-`.

## Supported ChartTypes

```C#
dance_single
dance_double
dance_couple
dance_solo
dance_threepanel
dance_routine
pump_single
pump_halfdouble
pump_double
pump_couple
pump_routine
kb7_single
ez2_single
ez2_double
ez2_real
para_single
ds3ddx_single
bm_single5
bm_versus5
bm_double5
bm_single7
bm_versus7
bm_double7
maniax_single
maniax_double
techno_single4
techno_single5
techno_single8
techno_double4
techno_double5
techno_double8
pnm_five
pnm_nine
lights_cabinet
kickbox_human
kickbox_quadarm
kickbox_insect
kickbox_arachnid

// These types are not supported in StepMania.
smx_beginner
smx_single
smx_dual
smx_full
smx_team
```