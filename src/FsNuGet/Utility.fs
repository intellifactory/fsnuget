namespace FsNuGet

open System
open System.IO
open System.Net
open System.Xml
open System.Xml.Linq
open SharpCompress

module internal Utility =

    let DownloadFile (url: string) =
        use client = new WebClient()
        client.DownloadData(url)
 
    let CreateUrl packageId version =
        sprintf "https://www.nuget.org/api/v2/package/%s/%s" packageId version

    let DownloadPackage packageId version =
        DownloadFile (CreateUrl packageId version)

    let EnsureDirExists path =
        let dir = DirectoryInfo(path)
        if not dir.Exists then
            dir.Create()

    let EnsureDirExistsFor path =
        EnsureDirExists (Path.GetDirectoryName(path))

    let DownloadPackageTo packageId version file =
        EnsureDirExistsFor file
        File.WriteAllBytes(file, DownloadPackage packageId version)

    let IsInternalEntry (path: string) =
        path.Contains("[Content_Types].xml")
        || path.StartsWith("package")
        || path.StartsWith("_rels")

    let UnzipToDirectory (skip: string -> bool) (data: byte[]) (dir: string) =
        let d = DirectoryInfo(dir)
        if d.Exists then
            for info in d.EnumerateDirectories() do
                info.Delete(``recursive`` = true)
            for info in d.EnumerateFiles() do
                info.Delete()
        else
            d.Create()
        let writeEntryToDirectory reader dir =
            let opts =
                Common.ExtractOptions.ExtractFullPath
                ||| Common.ExtractOptions.Overwrite
            Reader.IReaderExtensions.WriteEntryToDirectory(reader, dir, opts)
        use str = new MemoryStream(data)
        use reader = Reader.ReaderFactory.Open(str)
        while reader.MoveToNextEntry() do
            if not (skip reader.Entry.FilePath) then
                writeEntryToDirectory reader dir

    let ReadStream (s: Stream) =
        use m = new MemoryStream()
        s.CopyTo(m)
        m.ToArray()

    let ReadTextStream (s: Stream) =
        use r= new StreamReader(s)
        r.ReadToEnd()

    type Manifest =
        {
            Id : string
            Version : string
        }

    let ParseManifest (x: Stream) =
        let doc =
            ReadTextStream x
            |> XDocument.Parse
        let ns =
            [
                "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"
                "http://schemas.microsoft.com/packaging/2011/10/nuspec.xsd"
            ]
        let read name =
            let el =
                ns
                |> Seq.collect (fun ns -> doc.Descendants(XName.Get(name, ns)))
                |> Seq.head
            el.Value
        { Id = read "id"; Version = read "version" }

    let ReadManifestFromPackage (data: byte[]) =
        let out = ref None
        use str = new MemoryStream(data)
        use reader = Reader.ReaderFactory.Open(str)
        while reader.MoveToNextEntry() do
            if reader.Entry.FilePath.ToLower().EndsWith(".nuspec") then
                use manifest = reader.OpenEntryStream()
                out := Some (ParseManifest manifest)
        out.Value.Value
