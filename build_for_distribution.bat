set DEVENV=C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\devenv.exe
set ZIP=C:\Program Files\WinRAR\WinRAR.exe

"%DEVENV%" Fumen.sln /Clean
"%DEVENV%" Fumen.sln /Build Release

if exist Releases\StepManiaChartGenerator.zip (
    del Releases\StepManiaChartGenerator.zip
)

chdir StepManiaChartGenerator\bin\Release
"%ZIP%" a ..\..\..\Releases\StepManiaChartGenerator.zip *
chdir ..\..\..