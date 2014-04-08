#!/bin/bash

fsharpi --exec tools/configure.fsx
unzip build/DotNetZip.zip lib/net20/Ionic.Zip.dll
xbuild src/FsNuGet/FsNuGet.fsproj
