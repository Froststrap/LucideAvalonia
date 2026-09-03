namespace LucideAvalonia.Tools

open System
open System.Collections.Generic
open System.IO
open System.IO.Compression
open System.Net.Http
open System.Text
open LucideAvalonia.Tools.SvgConverter

module Program =
    let kebabToPascal (name: string) : string =
        let parts = name.Split('-')
        let result =
            parts
            |> Array.map (fun p ->
                if String.IsNullOrEmpty p then ""
                else Char.ToUpperInvariant(p.[0]).ToString() + p.Substring(1).ToLowerInvariant())
            |> String.concat ""
        if String.IsNullOrEmpty result then result
        elif Char.IsDigit result.[0] then "_" + result
        else result

    let generateAxaml (icons: seq<string * string list>) : string =
        let lines = ResizeArray<string>()
        lines.Add("<ResourceDictionary xmlns=\"https://github.com/avaloniaui\"")
        lines.Add("                    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">")

        for (name, drawings) in icons |> Seq.sortBy fst do
            if not (List.isEmpty drawings) then
                lines.Add(sprintf "    <DrawingImage x:Key=\"%s\">" name)
                lines.Add("        <DrawingImage.Drawing>")
                lines.Add("            <DrawingGroup ClipGeometry=\"M 0,0 L 24,0 24,24 0,24 Z\">")
                for drawing in drawings do
                    lines.Add(drawing)
                lines.Add("            </DrawingGroup>")
                lines.Add("        </DrawingImage.Drawing>")
                lines.Add("    </DrawingImage>")

        lines.Add("</ResourceDictionary>")
        String.concat "\n" lines + "\n"

    let generateEnum (names: seq<string>) : string =
        let sortedNames = names |> Seq.sort |> List.ofSeq
        let members = String.concat ",\n    " sortedNames
        sprintf "namespace LucideAvalonia.Enum;\n\npublic enum LucideIconNames\n{\n    %s\n}\n" members

    let downloadIcons (version: string) (dest: string) : string =
        let url = sprintf "https://github.com/lucide-icons/lucide/archive/refs/tags/%s.zip" version
        printfn "Downloading %s ..." url
        use client = new HttpClient()
        let data = client.GetByteArrayAsync(url).Result
        printfn "Downloaded %.1f MB" (float data.Length / 1024.0 / 1024.0)

        let zipPath = Path.Combine(dest, "lucide.zip")
        File.WriteAllBytes(zipPath, data)

        printfn "Extracting icons..."
        let iconsDir = Path.Combine(dest, "icons")
        Directory.CreateDirectory(iconsDir) |> ignore

        use fileStream = File.OpenRead(zipPath)
        use zf = new ZipArchive(fileStream, ZipArchiveMode.Read)
        for entry in zf.Entries do
            let fullName = entry.FullName
            if fullName.Contains("/icons/") && fullName.EndsWith(".svg") then
                let parts = fullName.Split([| "/icons/" |], StringSplitOptions.None)
                if parts.Length >= 2 then
                    let svgName = parts.[1]
                    if not (svgName.Contains "/") then
                        let target = Path.Combine(iconsDir, svgName)
                        use entryStream = entry.Open()
                        use targetStream = File.Create(target)
                        entryStream.CopyTo(targetStream)

        let svgCount = Directory.GetFiles(iconsDir, "*.svg").Length
        printfn "Extracted %d SVG files" svgCount
        iconsDir

    type Args =
        { Version: string option
          IconsDir: string option
          OutputDir: string option }

    let parseArgs (argv: string[]) : Args =
        let mutable version = None
        let mutable iconsDir = None
        let mutable outputDir = None
        let mutable i = 0
        while i < argv.Length do
            match argv.[i] with
            | "--version" ->
                i <- i + 1
                if i < argv.Length then version <- Some argv.[i]
            | "--icons-dir" ->
                i <- i + 1
                if i < argv.Length then iconsDir <- Some argv.[i]
            | "--output-dir" ->
                i <- i + 1
                if i < argv.Length then outputDir <- Some argv.[i]
            | _ -> ()
            i <- i + 1
        { Version = version; IconsDir = iconsDir; OutputDir = outputDir }

    [<EntryPoint>]
    let main argv =
        let args = parseArgs argv

        if args.Version.IsNone && args.IconsDir.IsNone then
            eprintfn "Error: Either --version or --icons-dir is required"
            eprintfn "Usage:"
            eprintfn "  dotnet run --project tools -- --version 0.473.0"
            eprintfn "  dotnet run --project tools -- --icons-dir ./path/to/svgs"
            exit 1

        if args.Version.IsSome && args.IconsDir.IsSome then
            eprintfn "Error: --version and --icons-dir are mutually exclusive"
            exit 1

        let scriptDir = __SOURCE_DIRECTORY__
        let parentDir = Directory.GetParent(scriptDir)
        
        let defaultOutput =
            match parentDir with
            | null -> Path.Combine(scriptDir, "LucideAvalonia")
            | p -> Path.Combine(p.FullName, "LucideAvalonia")
            
        let outputDir = defaultArg args.OutputDir defaultOutput

        let enumPath = Path.Combine(outputDir, "Enum", "LucideIconNames.cs")
        let axamlPath = Path.Combine(outputDir, "Lucide", "ResourcesIcons.axaml")

        let mutable tempDir: string option = None
        let iconsDir =
            match args.Version with
            | Some v ->
                let tmp = Path.Combine(Path.GetTempPath(), "lucide_" + Guid.NewGuid().ToString("N"))
                Directory.CreateDirectory(tmp) |> ignore
                tempDir <- Some tmp
                downloadIcons v tmp
            | None ->
                match args.IconsDir with
                | Some d -> d
                | None -> failwith "unreachable"

        if not (Directory.Exists iconsDir) then
            eprintfn "Error: Icons directory not found: %s" iconsDir
            exit 1

        let svgFiles = Directory.GetFiles(iconsDir, "*.svg") |> Array.sort
        if Array.isEmpty svgFiles then
            eprintfn "Error: No SVG files found in %s" iconsDir
            exit 1

        printfn "\nProcessing %d SVG files..." svgFiles.Length

        let icons = Dictionary<string, string list>()
        let skipped = ResizeArray<string>()

        for svgFile in svgFiles do
            let name = kebabToPascal (Path.GetFileNameWithoutExtension svgFile)
            let drawings = parseSvg svgFile
            if not (List.isEmpty drawings) then
                icons.[name] <- drawings
            else
                skipped.Add(Path.GetFileName svgFile)

        if skipped.Count > 0 then
            printfn "\nWarning: Skipped %d SVGs with no drawable elements:" skipped.Count
            for s in Seq.take (min 10 skipped.Count) skipped do
                printfn "  - %s" s
            if skipped.Count > 10 then
                printfn "  ... and %d more" (skipped.Count - 10)

        let nameMap = Dictionary<string, ResizeArray<string>>()
        for svgFile in svgFiles do
            let pascal = kebabToPascal (Path.GetFileNameWithoutExtension svgFile)
            if nameMap.ContainsKey pascal then
                nameMap.[pascal].Add(Path.GetFileNameWithoutExtension svgFile)
            else
                let ra = ResizeArray()
                ra.Add(Path.GetFileNameWithoutExtension svgFile)
                nameMap.[pascal] <- ra
        let dupes = nameMap |> Seq.filter (fun kv -> kv.Value.Count > 1) |> Seq.toList
        if not (List.isEmpty dupes) then
            printfn "\nWarning: Duplicate PascalCase names detected:"
            for kv in dupes do
                printfn "  %s <- %A" kv.Key (List.ofSeq kv.Value)

        printfn "\nGenerating %d icons..." icons.Count

        let axamlContent = generateAxaml (icons |> Seq.map (fun kv -> kv.Key, kv.Value))
        let enumContent = generateEnum (icons.Keys |> Seq.map id)

        let enumDir = Path.GetDirectoryName enumPath
        let axamlDir = Path.GetDirectoryName axamlPath
        
        if not (isNull enumDir) then Directory.CreateDirectory(enumDir) |> ignore
        if not (isNull axamlDir) then Directory.CreateDirectory(axamlDir) |> ignore

        File.WriteAllText(enumPath, enumContent, Encoding.UTF8)
        File.WriteAllText(axamlPath, axamlContent, Encoding.UTF8)

        printfn "\nOutput:"
        printfn "  %s (%d enum entries)" enumPath icons.Count
        printfn "  %s (%.0f KB)" axamlPath (float axamlContent.Length / 1024.0)

        printfn "\nSVG element breakdown:"
        for kv in elementCounts |> Seq.sortBy (fun kv -> kv.Key) do
            printfn "  <%s>: %d" kv.Key kv.Value

        match tempDir with
        | Some dir ->
            try Directory.Delete(dir, true) with _ -> ()
        | None -> ()

        printfn "\nDone!"
        0
