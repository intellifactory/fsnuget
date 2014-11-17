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

type PackageSource =
    | Online of url: string
    | FileSystem of path: string

[<AutoOpen>]
module PackageUtility =
    [<Literal>]
    let DefaultSourceUrl = "https://www.nuget.org/api/v2/"

    type Service = TypeProviders.ODataService<DefaultSourceUrl>
    type Pkg = Service.ServiceTypes.V2FeedPackage

    let dataFromPkg (pkg: Pkg) =
        {
            Bytes = Utility.DownloadPackage pkg.Id pkg.Version
            Id = pkg.Id
            Version = pkg.Version
        }

    let comparableVersion (v: string) =
        let v, prerel =
            match v.IndexOf '-' with
            | -1 -> v, string Char.MaxValue
            | n -> v.[..n-1], v.[n+1..]
        v.Split('.') |> Array.map int, prerel

    let tryGetLatestOData id url =
        let all =
            query {
                for p in Service.GetDataContext(url).Packages do
                where (p.Id = id)
                select p
            }
            |> Seq.toArray
        match all.Length with
        | 0 -> None
        | _ ->
            all
            |> Seq.maxBy (fun p -> comparableVersion p.NormalizedVersion)
            |> dataFromPkg
            |> Some

    let tryGetLatestFileSystem id path =
        let allFound =
            Directory.EnumerateFiles(path, id + ".*.nupkg")
            |> Seq.choose (fun path ->
                let version = Path.GetFileNameWithoutExtension(path).[id.Length + 1 ..]
                if not (Char.IsDigit version.[0]) then
                    // Searching for e.g. "WebSharper" we got e.g. "WebSharper.Owin.2.5.1.1.nupkg"
                    // so `version` above is "Owin.2.5.1.1"; discard
                    None
                else
                    Some (version, path)
            )
            |> List.ofSeq
        match allFound with
        | [] -> None
        | l ->
            let version, path = List.maxBy (fst >> comparableVersion) l
            Some {
                Bytes = File.ReadAllBytes(path)
                Id = id
                Version = version
            }

    let tryGetLatest id source =
        match source with
        | Online url -> tryGetLatestOData id (Uri url)
        | FileSystem path -> tryGetLatestFileSystem id path

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

    static member TryGetLatest(id, ?source) =
        let source = defaultArg source (Online DefaultSourceUrl)
        tryGetLatest id source
        |> Option.map (fun pkg -> { Data = pkg })

    static member GetLatest(id, ?source) =
        match Package.TryGetLatest(id, ?source = source) with
        | Some pkg -> pkg
        | None -> failwithf "Failed to find package by id %s" id

    member p.SaveToFile(file) =
        Utility.EnsureDirExistsFor file
        File.WriteAllBytes(file, p.Data.Bytes)

    member p.SaveToDirectory(dir) =
        p.SaveToFile(Path.Combine(dir, p.Text + ".nupkg"))

    member p.DataStream =
        new MemoryStream(p.Data.Bytes) :> Stream

