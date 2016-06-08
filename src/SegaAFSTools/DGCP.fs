module DGCP

open System
open System.IO
open System.Text

type private DGCPFileEntry = 
    {
        StringPartCount: int
        PartOffsetsOffset: int
    }

type DGCPFile(stringEntries: string array) = 
    new(s: Stream, encoding: Encoding) = 
        let br = new BinaryReader(s, encoding, true)
        let magic = br.ReadChars(4)
        assert(new string(magic) = "DGCP")

        let count = br.ReadInt32()
        br.ReadUInt32() |> ignore       // padding
        br.ReadUInt32() |> ignore       // padding

        let readNextFileEntry(_: int) = 
            {
                DGCPFileEntry.StringPartCount = br.ReadInt32()
                PartOffsetsOffset = br.ReadInt32()
            }

        // read null-terminated strings
        let readNextString(_: int) = 
            let readNextChar(c: char) = 
                if c = char 0 then
                    None
                else
                    Some((c, br.ReadChar()))

            new string(Seq.unfold(readNextChar)(br.ReadChar()) |> Seq.toArray)


        let entries = Array.init count readNextFileEntry
        let readPartsForEntry(e: DGCPFileEntry) = 
            br.BaseStream.Seek(int64 e.PartOffsetsOffset, SeekOrigin.Begin) |> ignore

            let readStringForPart(os: int) = 
                br.BaseStream.Seek(int64 os, SeekOrigin.Begin) |> ignore
                readNextString(0)

            Array.init(e.StringPartCount)(fun _ -> br.ReadInt32())
            |> Array.map readStringForPart
            
        let entries = 
            entries
            |> Array.map (fun e -> String.Join(" ", readPartsForEntry(e)))

        new DGCPFile(entries)

    new(filePath: string, encoding: Encoding) = 
        let fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
        new DGCPFile(fs, encoding)

    member this.Entries with get() = stringEntries