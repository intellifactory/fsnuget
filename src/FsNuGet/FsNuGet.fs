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

    type Service = TypeProviders.ODataService<DefaultSourceUrl, LocalSchemaFile = "schema.csdl">
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
            |> Some

    let tryGetOData id version url =
        query { for p in Service.GetDataContext(url).Packages do
                where (p.Id = id && p.NormalizedVersion = version)
                headOrDefault }
        |> function
            | null -> None
            | p -> Some (dataFromPkg p)

    let tryGetLatestFileSystem id path =
        let allFound =
            Directory.EnumerateFiles(path, id + ".*.nupkg", SearchOption.AllDirectories)
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
            Some (List.maxBy (fst >> comparableVersion) l)

    let tryGetFileSystem id version path =
        let paths = Directory.GetFiles(path, sprintf "%s.%s.nupkg" id version, SearchOption.AllDirectories)
        match List.ofArray paths with
        | [path] ->
            Some {
                Bytes = File.ReadAllBytes(path)
                Id = id
                Version = version
            }
        | _ -> None

    let tryGetLatest id source =
        match source with
        | Online url ->
            tryGetLatestOData id (Uri url)
            |> Option.map dataFromPkg
        | FileSystem path ->
            tryGetLatestFileSystem id path
            |> Option.map (fun (version, path) ->
                {
                    Bytes = File.ReadAllBytes(path)
                    Id = id
                    Version = version
                })

    let tryGet id version source =
        match source with
        | Online url -> tryGetOData id version (Uri url)
        | FileSystem path -> tryGetFileSystem id version path

    let tryFindLatestVersion id source =
        match source with
        | Online url ->
            tryGetLatestOData id (Uri url)
            |> Option.map (fun p -> p.NormalizedVersion)
        | FileSystem path ->
            tryGetLatestFileSystem id path
            |> Option.map fst

    let exists id source =
        match source with
        | Online url ->
            query { for p in Service.GetDataContext(Uri url).Packages do
                    exists (p.Id = id) }
        | FileSystem path ->
            tryGetLatestFileSystem id path
            |> Option.isSome

    let existsVersion id ver source =
        match source with
        | Online url ->
            query { for p in Service.GetDataContext(Uri url).Packages do
                    exists (p.Id = id && p.Version = ver) }
        | FileSystem path ->
            tryGetFileSystem id ver path 
            |> Option.isSome

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

    static member TryGetAtVersion(id, version, ?source) =
        let source = defaultArg source (Online DefaultSourceUrl)
        tryGet id version source
        |> Option.map (fun pkg -> { Data = pkg })

    static member GetAtVersion(id, version, ?source) =
        match Package.TryGetAtVersion(id, version, ?source = source) with
        | Some pkg -> pkg
        | None -> failwithf "Failed to find package by id %s, version %s" id version

    static member TryGetLatest(id, ?source) =
        let source = defaultArg source (Online DefaultSourceUrl)
        tryGetLatest id source
        |> Option.map (fun pkg -> { Data = pkg })

    static member GetLatest(id, ?source) =
        match Package.TryGetLatest(id, ?source = source) with
        | Some pkg -> pkg
        | None -> failwithf "Failed to find package by id %s" id

    static member TryFindLatestVersion(id, ?source) =
        let source = defaultArg source (Online DefaultSourceUrl)
        tryFindLatestVersion id source

    static member FindLatestVersion(id, ?source) =
        match Package.TryFindLatestVersion(id, ?source = source) with
        | Some pkg -> pkg
        | None -> failwithf "Failed to find package by id %s" id

    static member Exists(id, ?source) =
        let source = defaultArg source (Online DefaultSourceUrl)
        exists id source

    static member ExistsAtVersion(id, version, ?source) =
        let source = defaultArg source (Online DefaultSourceUrl)
        existsVersion id version source

    member p.SaveToFile(file) =
        Utility.EnsureDirExistsFor file
        File.WriteAllBytes(file, p.Data.Bytes)

    member p.SaveToDirectory(dir) =
        p.SaveToFile(Path.Combine(dir, p.Text + ".nupkg"))

    member p.DataStream =
        new MemoryStream(p.Data.Bytes) :> Stream

