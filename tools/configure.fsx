#load "../src/FsNuGet/Utility.fs"
open System
open System.IO

module U = FsNuGet.Utility

let loc path =
  Path.Combine(__SOURCE_DIRECTORY__, "..", path)

U.DownloadPackageTo "DotNetZip" "1.9.2" (loc "build/DotNetZip.zip")



