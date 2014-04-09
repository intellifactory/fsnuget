namespace FsNuGet

open System
open System.IO
open System.Net
open Microsoft.FSharp.Data
open SharpCompress

[<AutoOpen>]
module PackageUtility =
    type Service = TypeProviders.ODataService<"https://www.nuget.org/api/v2/">
    type Pkg = Service.ServiceTypes.V2FeedPackage

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
        | _ -> all |> Seq.maxBy (fun p -> p.LastUpdated) |> Some

    let install (pkg: Pkg) dir =
        let data = Utility.DownloadPackage pkg.Id pkg.Version
        Utility.UnzipToDirectory data dir

type Package =
    {
        Raw : Pkg
    }

    member p.Install(directory) =
        install p.Raw directory

    override p.ToString() =
        p.Text

    member p.Id = p.Raw.Id
    member p.Text = sprintf "%s.%s" p.Id p.Version
    member p.Version = p.Raw.Version

    static member TryGetLatest(id) =
        tryGetLatest id
        |> Option.map (fun pkg -> { Raw = pkg })

    static member GetLatest(id) =
        match Package.TryGetLatest(id) with
        | Some pkg -> pkg
        | None -> failwithf "Failed to find package by id %s" id
