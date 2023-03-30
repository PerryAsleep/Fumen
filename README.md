# 譜面
Fumen (譜面) is a library for representing various music game charts in a generic way.

## Projects
- [StepManiaEditor](StepManiaEditor/README.md): An editor for authoring [StepMania](https://www.stepmania.com/) charts.
- [StepManiaChartGenerator](StepManiaChartGenerator/README.md): An application for converting [StepMania](https://www.stepmania.com/) charts into other StepMania charts.
### Misc
- [PadDataGenerator](PadDataGenerator/README.md): An application for authoring PadData and StepGraph files.
- [ChartGeneratorTests](ChartGeneratorTests/README.md): Unit test project for `StepManiaChartGenerator`.
- [ExpressedChartTestGenerator](ExpressedChartTestGenerator/README.md): An application to generate `ChartGeneratorTests` tests.
- [ChartStats](ChartStats/README.md): An application to generate `csv` files with doubles chart statistics.

## Building From Source
Building from source requires Windows 10 or greater and Microsoft Visual Studio Community 2022.

Clone the repository and update submodules.
```
git clone https://github.com/PerryAsleep/Fumen.git
git submodule update --init
```

Add an environment variable for `FUMEN_DEVENV` set to the path of your Visual Studio `devenv.exe`, e.g. `C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe`.
