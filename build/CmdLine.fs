namespace BlackFox.CommandLine

/// Escape arguments in a form that programs parsing it as Microsoft C Runtime will successfuly understand
/// Rules taken from http://www.daviddeley.com/autohotkey/parameters/parameters.htm#WINARGV
module MsvcrCommandLine =
    open System.Text

    let escapeArg (arg : string) (builder : StringBuilder) =
        let needQuote = arg.Contains(" ") || arg.Contains("\t")
        let rec escape (builder: StringBuilder) pos =
            if pos >= arg.Length then
                ()
            else
                let c = arg.[pos]
                let isLast = pos = arg.Length-1
                match c with
                | '"' -> // Quotes are escaped
                    escape (builder.Append(@"\""")) (pos + 1)
                | '\\' when isLast && needQuote -> // Backslash ending a quoted arg need escape
                    escape (builder.Append(@"\\")) (pos + 1)
                | '\\' when not isLast -> // Backslash followed by quote need to be escaped
                    let nextC = arg.[pos+1]
                    match nextC with
                    | '"' ->
                        escape (builder.Append(@"\\\""")) (pos + 2)
                    | _ ->
                        escape (builder.Append(c)) (pos + 1)
                | _ ->
                    escape (builder.Append(c)) (pos + 1)

        if needQuote then builder.Append('"') |> ignore
        escape builder 0
        if needQuote then builder.Append('"') |> ignore

type CmdLineArgType = | Normal of string | Raw of string

type CmdLine = {
    Args: CmdLineArgType list
    }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module CmdLine =
    open System
    open System.Text

    let empty = { Args = List.empty }

    let inline appendRaw value (cmdLine : CmdLine) =
        { cmdLine with Args = Raw(value) :: cmdLine.Args }

    let inline appendRawIfSome value (cmdLine : CmdLine) =
        match value with
        | Some(value) -> appendRaw value cmdLine
        | None -> cmdLine

    let inline concat (other : CmdLine) (cmdLine : CmdLine) =
        { cmdLine with Args = other.Args @ cmdLine.Args }

    let inline append (value : obj) (cmdLine : CmdLine) =
        let s =
            match value with
            | :? String as sv -> sv
            | _ -> sprintf "%A" value

        { cmdLine with Args = Normal(s) :: cmdLine.Args }

    let inline fromSeq (values : string seq) =
        values |> Seq.fold (fun state o -> append o state) empty

    let inline appendIfTrue cond value cmdLine =
        if cond then cmdLine |> append value else cmdLine

    let inline appendIfSome value cmdLine =
        match value with
        | Some(value) -> cmdLine |> append value
        | None -> cmdLine

    let inline appendIfNotNullOrEmpty value s cmdLine =
        appendIfTrue (not (String.IsNullOrEmpty(value))) (s+value) cmdLine

    let inline private argsInOrder cmdLine = cmdLine.Args |> List.rev

    let private escape escapeFun cmdLine =
        let builder = StringBuilder()
        cmdLine |> argsInOrder |> Seq.iteri (fun i arg ->
            if (i <> 0) then builder.Append(' ') |> ignore
            match arg with
            | Normal(arg) -> escapeFun arg builder
            | Raw(arg) -> builder.Append(arg) |> ignore)

        builder.ToString()

    let inline toStringForMsvcr cmdLine = escape MsvcrCommandLine.escapeArg cmdLine

    let inline toString cmdLine = toStringForMsvcr cmdLine
