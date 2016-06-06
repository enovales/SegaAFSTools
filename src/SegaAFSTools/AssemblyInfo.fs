namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("SegaAFSTools")>]
[<assembly: AssemblyProductAttribute("SegaAFSTools")>]
[<assembly: AssemblyDescriptionAttribute("Tools for working with AFS archive files, as appearing in games by and for Sega")>]
[<assembly: AssemblyVersionAttribute("1.0")>]
[<assembly: AssemblyFileVersionAttribute("1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0"
    let [<Literal>] InformationalVersion = "1.0"
