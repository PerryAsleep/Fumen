@REM Tools. Hard-coded and not included in repo. Brittle.
set DEVENV=C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\devenv.exe
set ZIP=C:\Program Files\WinRAR\WinRAR.exe

@REM Clean and build the solution.
"%DEVENV%" Fumen.sln /Clean
"%DEVENV%" Fumen.sln /Build Release

@REM Remove any existing package.
if exist Releases\StepManiaChartGenerator.zip (
    del Releases\StepManiaChartGenerator.zip
)

@REM Copy the config for shipping into the bin directory before packaging.
xcopy .\StepManiaChartGenerator\config-ship.json .\StepManiaChartGenerator\bin\Release\config.json /Y

@REM Zip the contents of the bin directory into a new package.
chdir StepManiaChartGenerator\bin\Release
"%ZIP%" a ..\..\..\Releases\StepManiaChartGenerator.zip *
chdir ..\..\..