﻿
#r "bin/Debug/FsNuGet.dll"
open FsNuGet

let pkg = Package.GetLatest("WebSharper")

pkg.Install(__SOURCE_DIRECTORY__ + "/pkg")