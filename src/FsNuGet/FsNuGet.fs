namespace FsNuGet

open System
open System.IO
open System.Net
open Microsoft.FSharp.Data
open SharpCompress

[<NoComparison>]
[<NoEquality>]
type PackageData =
    {
        Bytes : byte []
        Id : string
        Version : string
    }

[<AutoOpen>]
module PackageUtility =
    type Service = TypeProviders.ODataService<"https://www.nuget.org/api/v2/">
    type Pkg = Service.ServiceTypes.V2FeedPackage

    let dataFromPkg (pkg: Pkg) =
        {
            Bytes = Utility.DownloadPackage pkg.Id pkg.Version
            Id = pkg.Id
            Version = pkg.Version
        }

    let tryGetLatest id =
        let all =
            query {
                for p in Service.GetDataContext().Packages do
                where (p.Id = id)
                select p
            }
            |> Seq.toArray
        match all.Length with
        | 0 -> None
        | _ -> all |> Seq.maxBy (fun p -> p.LastUpdated) |> dataFromPkg |> Some

    let install data dir =
        Utility.UnzipToDirectory Utility.IsInternalEntry data.Bytes dir

    let readBytes bytes =
        let manifest = Utility.ReadManifestFromPackage(bytes)
        {
            Bytes = Array.copy bytes
            Id = manifest.Id
            Version = manifest.Version
        }

type Package =
    {
        Data : PackageData
    }

    member p.Install(directory) =
        install p.Data directory

    override p.ToString() =
        p.Text

    member p.Id = p.Data.Id
    member p.Text = sprintf "%s.%s" p.Id p.Version
    member p.Version = p.Data.Version

    static member FromStream(s: Stream) =
        Utility.ReadStream s
        |> Package.FromBytes

    static member FromBytes(bytes) =
        { Data = readBytes bytes }

    static member FromFile(path: string) =
        File.ReadAllBytes(path)
        |> Package.FromBytes

    static member TryGetLatest(id) =
        tryGetLatest id
        |> Option.map (fun pkg -> { Data = pkg })

    static member GetLatest(id) =
        match Package.TryGetLatest(id) with
        | Some pkg -> pkg
        | None -> failwithf "Failed to find package by id %s" id

    member p.SaveToFile(file) =
        Utility.EnsureDirExistsFor file
        File.WriteAllBytes(file, p.Data.Bytes)

    member p.SaveToDirectory(dir) =
        p.SaveToFile(Path.Combine(dir, p.Text + ".nupkg"))

