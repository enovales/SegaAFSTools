// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
module AFSTool

open AFS
open CommandLine
open System
open System.IO

[<CommandLine.Verb("examine", HelpText = "Examine AFS archives and dump information about them")>]
type ExamineConfiguration() = 
    [<
      CommandLine.Option(
        "archive-path",
        Required = true,
        HelpText = "The path of the archive or archives you want to examine"
      )
    >]
    member val ArchivePath = "" with get, set

    [<
      CommandLine.Option(
        "destination-path", 
        Required = false, 
        HelpText = "The path to which the output will be dumped. If not specified, output will be written to the screen."
      )
    >]
    member val DestinationPath = "" with get, set

    [<
      CommandLine.Option(
        "recurse",
        Required = false,
        HelpText = "If enabled, and extracting a directory, the directory structure will be searched recursively for archives."
      )
    >]
    member val Recurse = false with get, set

/// <summary>
/// Configuration options for archive extraction
/// </summary>
[<CommandLine.Verb("extract", HelpText = "Extract content from an AFS archive")>]
type ExtractConfiguration() = 
    [<
      CommandLine.Option(
        "archive-path", 
        Required = true, 
        HelpText = "The path of the archive or archives you want to extract"
      )
    >]
    member val ArchivePath = "" with get, set

    [<
      CommandLine.Option(
        "destination-path", 
        Required = true, 
        HelpText = "The path to which the archives will be extracted. If multiple archives are extracted, each one will be placed in its own subdirectory."
      )
    >]
    member val DestinationPath = "" with get, set

    [<
      CommandLine.Option(
        "recurse",
        Required = false,
        HelpText = "If enabled, and extracting a directory, the directory structure will be searched recursively for archives."
      )
    >]
    member val Recurse = false with get, set

let private extractSingleArchive(destinationPath: string, makeDirectory: bool)(archive: string) = 
    let extractPath = 
        if makeDirectory then
            Path.Combine(destinationPath, Path.GetFileNameWithoutExtension(archive))
        else
            destinationPath

    if not(Directory.Exists(extractPath)) then
        Directory.CreateDirectory(extractPath) |> ignore

    use s = new FileStream(archive, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
    use afs = new AFSArchive(s)

    let extractSingleFile(i: int)(fe: ArchiveFileEntry) = 
        let filePath = Path.Combine(extractPath, fe.Name + "___" + i.ToString("00000"))
        use sourceStream = afs.GetStream(fe)
        use destStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)
        sourceStream.CopyTo(destStream)

    afs.FileEntries
    |> Array.iteri extractSingleFile

let private examineSingleArchive(otw: TextWriter)(archive: string) = 
    use s = new FileStream(archive, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
    use afs = new AFSArchive(s)

    let dumpSingleEntry(afe: ArchiveFileEntry) = 
        otw.WriteLine(String.Format("{0,32} {1,16} {2}", afe.Name, afe.Length, afe.Offset.ToString("X8")))

    afs.FileEntries
    |> Array.iter dumpSingleEntry

let private runExtract(c: ExtractConfiguration) = 
    let isMultipleExtraction = Directory.Exists(c.ArchivePath)
    let archives = 
        if isMultipleExtraction then
            let searchOptions = 
                match c.Recurse with
                | true -> SearchOption.AllDirectories
                | false -> SearchOption.TopDirectoryOnly

            Directory.GetFiles(c.ArchivePath, "*.afs", searchOptions)
        else
            [| c.ArchivePath |]

    archives
    |> Array.iter(extractSingleArchive(c.DestinationPath, isMultipleExtraction))

let private runExamine(c: ExamineConfiguration) = 
    let isMultipleExamination = Directory.Exists(c.ArchivePath)
    let archives = 
        if isMultipleExamination then
            let searchOptions = 
                match c.Recurse with
                | true -> SearchOption.AllDirectories
                | false -> SearchOption.TopDirectoryOnly

            Directory.GetFiles(c.ArchivePath, "*.afs", searchOptions)
        else
            [| c.ArchivePath |]

    let outputWriter = 
        if (String.IsNullOrWhiteSpace(c.DestinationPath)) then
            System.Console.Out
        else
            new StreamWriter(c.DestinationPath) :> TextWriter

    outputWriter.WriteLine(String.Format("{0,32} {1,16} {2}", "Filename", "Length", "Offset"))
    outputWriter.WriteLine(new String('-', 80))
    archives
    |> Array.iter(examineSingleArchive(outputWriter))

[<EntryPoint>]
let main argv = 
    let commandLineParserSetupAction(t: CommandLine.ParserSettings) = 
        t.HelpWriter <- System.Console.Out
        t.EnableDashDash <- true
        t.IgnoreUnknownArguments <- true

    let commandLineParser = new CommandLine.Parser(new Action<CommandLine.ParserSettings>(commandLineParserSetupAction))
    commandLineParser
        .ParseArguments<ExtractConfiguration, ExamineConfiguration>(argv)
        .WithParsed<ExtractConfiguration>(new Action<ExtractConfiguration>(runExtract))
        .WithParsed<ExamineConfiguration>(new Action<ExamineConfiguration>(runExamine))
    |> ignore

    0 // return an integer exit code
