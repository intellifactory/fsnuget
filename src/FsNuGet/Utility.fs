namespace FsNuGet

open System
open System.IO
open System.Net

module internal Utility =

    let DownloadFile (url: string) =
        use client = new WebClient()
        client.DownloadData(url)
 
    let CreateUrl packageId version =
        sprintf "https://www.nuget.org/api/v2/package/%s/%s" packageId version

    let DownloadPackage packageId version =
        DownloadFile (CreateUrl packageId version)

    let EnsureDirExistsFor path =
        let dir = DirectoryInfo(Path.GetDirectoryName(path))
        if not dir.Exists then
            dir.Create()

    let DownloadPackageTo packageId version file =
        EnsureDirExistsFor file
        File.WriteAllBytes(file, DownloadPackage packageId version)
