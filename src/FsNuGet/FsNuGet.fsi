namespace FsNuGet

open System
open System.IO

type PackageSource =
    | Online of url: string
    | FileSystem of path: string

/// Represents a NuGet package at a specific verison.
[<Sealed>]
type Package =

    /// Downloads the package contents and unpacks to a folder on disk.
    /// For example, `p.Install("packages/" + p.Text)`, will create
    /// `packages/MyPkg.1.2.3` folder with the contents of the package.
    member Install : directory: string -> unit

    /// Writes this package as a nupkg file in a given directory.
    member SaveToDirectory : directory: string -> unit

    /// Writes this package in nupkg format at a given path.
    member SaveToFile : path: string -> unit

    /// Returns the package data in nupkg format as a stream.
    member DataStream : Stream

    /// Package identity, such as "NuGet.Core".
    member Id : string

    /// Text representation, such as "NuGet.Core.2.8.0".
    member Text : string

    /// Package version, such as "2.8.0".
    member Version : string

    /// Reads raw bytes in nupkg format.
    static member FromBytes : bytes: byte [] -> Package

    /// Reads a given nupkg file.
    static member FromFile : path: string -> Package

    /// Reads nupkg format from a stream.
    static member FromStream : Stream -> Package

    /// Like `Package.TryGetAtVersion`, but throws an exception on failure.
    static member GetAtVersion : id: string * version: string * ?source: PackageSource -> Package

    /// Attempts to find the package with the given id and version.
    /// When `source` is not specified, searches the official NuGet repository.
    static member TryGetAtVersion : id: string * version: string * ?source: PackageSource -> option<Package>

    /// Like `Package.TryGetLatest`, but throws an exception on failure.
    static member GetLatest : id: string * ?source: PackageSource -> Package

    /// Attempts to find the latest version of a package by id.
    /// When `source` is not specified, searches the official NuGet repository.
    static member TryGetLatest : id: string * ?source: PackageSource -> option<Package>
