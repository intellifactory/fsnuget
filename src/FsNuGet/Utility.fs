namespace FsNuGet

open System
open System.IO
open System.Net
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

    let UnzipToDirectory (data: byte[]) (dir: string) =
        let writeEntryToDirectory reader dir =
            let opts =
                Common.ExtractOptions.ExtractFullPath
                ||| Common.ExtractOptions.Overwrite
            Reader.IReaderExtensions.WriteEntryToDirectory(reader, dir, opts)
        EnsureDirExists dir
        use str = new MemoryStream(data)
        use reader = Reader.ReaderFactory.Open(str)
        while reader.MoveToNextEntry() do
            writeEntryToDirectory reader dir
