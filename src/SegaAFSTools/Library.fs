module AFS

open System
open System.Diagnostics
open System.IO
open System.Text

type ArchiveRawFileEntry = {
    Offset: uint32
    Length: uint32
}

type ArchiveRawFilenameEntry = {
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
}

type AFSArchive(s: Stream, br: BinaryReader, entries: ArchiveFileEntry array) = 
    new(s: Stream) = 
        let br = new BinaryReader(s, Encoding.UTF8, true)

        // read magic header
        if br.ReadBytes(4) <> [| byte 0x41; byte 0x46; byte 0x53; byte 0x00 |] then
            raise(exn("invalid magic header"))

        // read number of files
        let numberOfFiles = br.ReadInt32()

        // read file entries.
        // theoretical maximum file entry count: (0x80000 - 0x4 (header) - 0x8 (offset of file name table)) / 0x8
        let maximumFileEntries = (0x80000 - 0x4 - 0x8) / 0x8
        let readNextFileEntry = (fun (n: int) -> 
                if (n + 1) < maximumFileEntries then
                    let nextRecord = { ArchiveRawFileEntry.Offset = br.ReadUInt32(); Length = br.ReadUInt32() }
                    match nextRecord with
                    | nr when nr.Offset = uint32 0 && nr.Length = uint32 0 -> None
                    | _ -> Some((nextRecord, n + 1))
                else
                    None
            )
        let rawFileEntries = Seq.unfold readNextFileEntry 0 |> Array.ofSeq

        // read filename entries
        let filenameTableOffsetOffset = int64 0x7FFF8
        if s.Seek(filenameTableOffsetOffset, SeekOrigin.Begin) <> filenameTableOffsetOffset then
            raise(exn("invalid filename table offset offset"))
        let filenameTableOffset = br.ReadUInt32()

        if s.Seek(int64 filenameTableOffset, SeekOrigin.Begin) <> int64 filenameTableOffset then
            raise(exn("invalid filename table offset"))

        let readNextFilenameEntry(numEntries: int) = (fun (n: int) ->
                if n >= numEntries then
                    None
                else
                    let filenameEntry = {
                        ArchiveRawFilenameEntry.Name = Encoding.UTF8.GetString(br.ReadBytes(32)).TrimEnd(char 0)
                        Year = br.ReadUInt16()
                        Month = br.ReadUInt16()
                        Day = br.ReadUInt16()
                        Hour = br.ReadUInt16()
                        Minute = br.ReadUInt16()
                        Second = br.ReadUInt16()
                        Length = br.ReadUInt32()
                    }
                    Some((filenameEntry, n + 1))
            )
        let rawFilenameEntries = Seq.unfold(readNextFilenameEntry(numberOfFiles))(0) |> Array.ofSeq

        if (rawFilenameEntries.Length <> rawFileEntries.Length) then
            raise(exn("mismatch in number of file entries and filename entries"))

        let fileEntries = rawFilenameEntries |> Array.zip(rawFileEntries) |> Array.map(fun (fe, fne) -> 
            {
                ArchiveFileEntry.Name = fne.Name
                Offset = fe.Offset
                Length = fe.Length
            })
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

