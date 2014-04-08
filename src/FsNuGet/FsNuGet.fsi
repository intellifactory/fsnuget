namespace FsNuGet

/// Represents a NuGet package at a specific verison.
[<Sealed>]
type Package =

    /// Downloads the package contents and unpacks to a folder on disk.
    /// For example, `p.Install("packages/" + p.Text)`, will create
    /// `packages/MyPkg.1.2.3` folder with the contents of the package.
    member Install : directory: string -> unit

    /// Package identity, such as "NuGet.Core".
    member Id : string

    /// Text representation, such as "NuGet.Core.2.8.0".
    member Text : string

    /// Package version, such as "2.8.0".
    member Version : string

    /// Like `Package.TryGetLatest`, but throws an exception on failure.
    static member GetLatest : id: string -> Package

    /// Attempts to find the latest version of a package by id.
    /// Searches the official NuGet repository only.
    static member TryGetLatest : id: string -> option<Package>
