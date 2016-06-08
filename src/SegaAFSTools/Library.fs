module AFS

open System
open System.Diagnostics
open System.IO
open System.Text

type private RawFileEntry = {
    Offset: uint32
    Length: uint32
}

type private RawFilenameEntry = {
    Name: string
    Year: uint16
    Month: uint16
    Day: uint16
    Hour: uint16
    Minute: uint16
    Second: uint16
    Length: uint32
}

type ArchiveFileEntry = {
    Name: string
    Offset: uint32
    Length: uint32
    Year: uint16 option
    Month: uint16 option
    Day: uint16 option
    Hour: uint16 option
    Minute: uint16 option
    Second: uint16 option
}

let private readArchiveRawFilenameEntry(br: BinaryReader) = 
    let nameBuf = br.ReadBytes(32)
    let nullIndex = Array.IndexOf(nameBuf, byte 0)
    let nameString = 
        if nullIndex >= 0 then
            Encoding.UTF8.GetString(nameBuf, 0, nullIndex)
        else
            Encoding.UTF8.GetString(nameBuf)
    {
        RawFilenameEntry.Name = nameString
        Year = br.ReadUInt16()
        Month = br.ReadUInt16()
        Day = br.ReadUInt16()
        Hour = br.ReadUInt16()
        Minute = br.ReadUInt16()
        Second = br.ReadUInt16()
        Length = br.ReadUInt32()
    }


type AFSArchive internal (s: Stream, br: BinaryReader, entries: ArchiveFileEntry array) = 
    new(s: Stream) = 
        let br = new BinaryReader(s, Encoding.UTF8, true)

        // read magic header
        if br.ReadBytes(4) <> [| byte 0x41; byte 0x46; byte 0x53; byte 0x00 |] then
            raise(exn("invalid magic header"))

        // read number of files
        let numberOfFiles = br.ReadInt32()

        // read file entries.
        let readNextFileEntry = (fun (_: int) -> { RawFileEntry.Offset = br.ReadUInt32(); Length = br.ReadUInt32() })
        let rawFileEntries = Seq.init(numberOfFiles) readNextFileEntry |> Array.ofSeq

        // filename table and length is the next entry.
        let filenameTableOffset = br.ReadUInt32()
        //let filenameTableLength = br.ReadUInt32()

        // read filename entries
        if s.Seek(int64 filenameTableOffset, SeekOrigin.Begin) <> int64 filenameTableOffset then
            raise(exn("invalid filename table offset"))

        let readNextFilenameEntry(numEntries: int) = (fun (n: int) ->
                if n >= numEntries then
                    None
                else
                    Some((readArchiveRawFilenameEntry(br), n + 1))
            )
        let rawFilenameEntries = Seq.unfold(readNextFilenameEntry(numberOfFiles))(0) |> Array.ofSeq

        if (rawFilenameEntries.Length <> rawFileEntries.Length) then
            raise(exn("mismatch in number of file entries and filename entries"))

        let makeArchiveFileEntry(fe: RawFileEntry, fne: RawFilenameEntry) = 
            {
                ArchiveFileEntry.Name = fne.Name
                Offset = fe.Offset
                Length = fe.Length
                Year = Some(fne.Year)
                Month = Some(fne.Month)
                Day = Some(fne.Day)
                Hour = Some(fne.Hour)
                Minute = Some(fne.Minute)
                Second = Some(fne.Second)
            }

        let fileEntries = 
            rawFilenameEntries 
            |> Array.zip(rawFileEntries) 
            |> Array.map makeArchiveFileEntry

        new AFSArchive(s, br, fileEntries)

    new(filename: string) = 
        new AFSArchive(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))

    member this.FileEntries with get() = entries
    member this.GetStream(fe: ArchiveFileEntry) = 
        Debug.Assert(entries |> Array.contains(fe), "file entry doesn't belong to this archive")
        let newPos = s.Seek(int64 fe.Offset, SeekOrigin.Begin)
        if (newPos <> int64 fe.Offset) then
            raise(exn(sprintf "could not seek to file offset %d in file %s" fe.Offset fe.Name))

        let resultBytes = br.ReadBytes(int fe.Length)
        if (resultBytes.Length <> int fe.Length) then
            raise(exn(sprintf "could not read entire file size %d in file %s" fe.Length fe.Name))

        new MemoryStream(resultBytes)

    member this.GetBinaryReader(fe: ArchiveFileEntry) = 
        new BinaryReader(this.GetStream(fe))

    interface IDisposable with
        member this.Dispose() = 
            s.Dispose()

