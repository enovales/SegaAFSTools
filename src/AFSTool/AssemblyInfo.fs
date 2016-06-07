namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("AFSTool")>]
[<assembly: AssemblyProductAttribute("SegaAFSTools")>]
[<assembly: AssemblyDescriptionAttribute("Tools for working with AFS archive files, as appearing in games by and for Sega")>]
[<assembly: AssemblyVersionAttribute("0.0.1")>]
[<assembly: AssemblyFileVersionAttribute("0.0.1")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.0.1"
    let [<Literal>] InformationalVersion = "0.0.1"
