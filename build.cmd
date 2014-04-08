@ECHO OFF
setlocal
set PATH=%PATH%;%ProgramFiles(x86)%\Microsoft SDKs\F#\3.1\Framework\v4.0
set PATH=%PATH%;%ProgramFiles%\Microsoft SDKs\F#\3.1\Framework\v4.0
set PATH=%PATH%;%WINDIR%\Microsoft.NET\Framework\v4.0.30319
tools\NuGet.exe install DotNetZip -o packages -excludeVersion
MSBuild.exe src/FsNuGet/FsNuGet.fsproj /p:Configuration=Release /p:VisualStudioVersion=12.0
