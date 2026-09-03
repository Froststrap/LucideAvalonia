namespace LucideAvalonia.Tools

open System
open System.Collections.Generic
open System.Xml.Linq

module SvgConverter =
    let elementCounts = Dictionary<string, int>()

    let trackStat (tag: string) =
        match elementCounts.TryGetValue tag with
        | true, c -> elementCounts.[tag] <- c + 1
        | false, _ -> elementCounts.[tag] <- 1

    let parsePoints (pointsStr: string) : (string * string) list =
        let nums = pointsStr.Replace(",", " ").Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
        [ for i in 0..2..(nums.Length - 2) -> nums.[i], nums.[i + 1] ]

    let escapeXmlAttr (s: string) : string =
        s.Replace("&", "&amp;")
         .Replace("\"", "&quot;")
         .Replace("<", "&lt;")
         .Replace(">", "&gt;")

    let penXml =
        "                    <GeometryDrawing.Pen>\n"
        + "                        <Pen Brush=\"Black\" Thickness=\"2\" LineCap=\"Round\" LineJoin=\"Round\" />\n"
        + "                    </GeometryDrawing.Pen>"

    let attrOpt (elem: XElement) (name: string) : string option =
        match elem.Attribute(XName.Get name) with
        | null -> None
        | a -> Some a.Value

    let attrDef (elem: XElement) (name: string) (def: string) : string =
        defaultArg (attrOpt elem name) def

    let convertElement (elem: XElement) : string option =
        let tag = elem.Name.LocalName
        trackStat tag

        match tag with
        | "path" ->
            let d = attrDef elem "d" ""
            if String.IsNullOrEmpty d then None
            else
                let geom = escapeXmlAttr ("F1 " + d)
                Some (sprintf "                <GeometryDrawing Geometry=\"%s\">\n%s\n                </GeometryDrawing>" geom penXml)

        | "circle" ->
            let cx, cy, r = attrDef elem "cx" "0", attrDef elem "cy" "0", attrDef elem "r" "0"
            Some (sprintf "                <GeometryDrawing>\n%s\n                    <GeometryDrawing.Geometry>\n                        <EllipseGeometry RadiusX=\"%s\" RadiusY=\"%s\" Center=\"%s,%s\" />\n                    </GeometryDrawing.Geometry>\n                </GeometryDrawing>" penXml r r cx cy)

        | "ellipse" ->
            let cx, cy = attrDef elem "cx" "0", attrDef elem "cy" "0"
            let rxOpt, ryOpt = attrOpt elem "rx", attrOpt elem "ry"
            let rx, ry =
                match rxOpt, ryOpt with
                | Some rx, None -> rx, rx
                | None, Some ry -> ry, ry
                | _ -> defaultArg rxOpt "0", defaultArg ryOpt "0"
            Some (sprintf "                <GeometryDrawing>\n%s\n                    <GeometryDrawing.Geometry>\n                        <EllipseGeometry RadiusX=\"%s\" RadiusY=\"%s\" Center=\"%s,%s\" />\n                    </GeometryDrawing.Geometry>\n                </GeometryDrawing>" penXml rx ry cx cy)

        | "rect" ->
            let x, y = attrDef elem "x" "0", attrDef elem "y" "0"
            let w, h = attrDef elem "width" "0", attrDef elem "height" "0"
            let rxOpt, ryOpt = attrOpt elem "rx", attrOpt elem "ry"
            let rx, ry =
                match rxOpt, ryOpt with
                | Some rx, None -> rx, rx
                | None, Some ry -> ry, ry
                | _ -> defaultArg rxOpt "0", defaultArg ryOpt "0"
            Some (sprintf "                <GeometryDrawing>\n%s\n                    <GeometryDrawing.Geometry>\n                        <RectangleGeometry RadiusX=\"%s\" RadiusY=\"%s\" Rect=\"%s,%s,%s,%s\" />\n                    </GeometryDrawing.Geometry>\n                </GeometryDrawing>" penXml rx ry x y w h)

        | "line" ->
            let x1, y1 = attrDef elem "x1" "0", attrDef elem "y1" "0"
            let x2, y2 = attrDef elem "x2" "0", attrDef elem "y2" "0"
            Some (sprintf "                <GeometryDrawing>\n%s\n                    <GeometryDrawing.Geometry>\n                        <LineGeometry StartPoint=\"%s,%s\" EndPoint=\"%s,%s\" />\n                    </GeometryDrawing.Geometry>\n                </GeometryDrawing>" penXml x1 y1 x2 y2)

        | "polyline" ->
            let points = parsePoints (attrDef elem "points" "")
            if List.isEmpty points then None
            else
                let first = List.head points
                let pathData =
                    List.fold (fun acc (px, py) -> acc + sprintf " L%s %s" px py)
                        (sprintf "M%s %s" (fst first) (snd first))
                        (List.tail points)
                let geom = escapeXmlAttr ("F1 " + pathData)
                Some (sprintf "                <GeometryDrawing Geometry=\"%s\">\n%s\n                </GeometryDrawing>" geom penXml)

        | "polygon" ->
            let points = parsePoints (attrDef elem "points" "")
            if List.isEmpty points then None
            else
                let first = List.head points
                let pathData =
                    List.fold (fun acc (px, py) -> acc + sprintf " L%s %s" px py)
                        (sprintf "M%s %s" (fst first) (snd first))
                        (List.tail points)
                    + " Z"
                let geom = escapeXmlAttr ("F1 " + pathData)
                Some (sprintf "                <GeometryDrawing Geometry=\"%s\">\n%s\n                </GeometryDrawing>" geom penXml)

        | _ -> None

    let drawableTags = set [ "path"; "circle"; "ellipse"; "rect"; "line"; "polyline"; "polygon" ]

    let rec collectElements (parent: XElement) : XElement list =
        [
            for child in parent.Elements() do
                let tag = child.Name.LocalName
                if tag = "g" then
                    yield! collectElements child
                elif Set.contains tag drawableTags then
                    yield child
        ]

    let parseSvg (filepath: string) : string list =
        let doc = XDocument.Load(filepath)
        match doc.Root with
        | null -> []
        | root -> collectElements root |> List.choose convertElement
