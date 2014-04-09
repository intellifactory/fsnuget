#r "bin/Release/FsNuGet.dll"
open System
open System.IO
open FsNuGet

let p = Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "packages", "sharpcompress", "sharpcompress.nupkg")
let ok =
    [
        Package.FromFile(p).Id = "sharpcompress"
    ]
    |> Seq.forall (fun x -> x)
