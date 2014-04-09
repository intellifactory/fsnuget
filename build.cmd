@ECHO OFF
setlocal
set PATH=%PATH%;%ProgramFiles(x86)%\Microsoft SDKs\F#\3.1\Framework\v4.0
set PATH=%PATH%;%ProgramFiles%\Microsoft SDKs\F#\3.1\Framework\v4.0
set PATH=%PATH%;%WINDIR%\Microsoft.NET\Framework\v4.0.30319
tools\NuGet.exe install DotNetZip.Reduced -version 1.9.1.8 -o packages -excludeVersion
MSBuild.exe src/FsNuGet/FsNuGet.fsproj /p:Configuration=Release /p:VisualStudioVersion=12.0
if not exist build mkdir build
tools\NuGet.exe pack FsNuGet.nuspec -outputDirectory build -Version %APPVEYOR_BUILD_VERSION%
