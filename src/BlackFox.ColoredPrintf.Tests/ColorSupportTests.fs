module BlackFox.ColoredPrintf.Tests.ColorSupportTests

open BlackFox.ColoredPrintf.ColorSupport
open Expecto

let private envOf (values: (string * string) list) name =
    values |> List.tryFind (fst >> (=) name) |> Option.map snd |> Option.toObj

[<Tests>]
let colorSupportTests =
    testList "ColorSupport" [
        testCase "No env vars, not redirected -> supported" <| fun _ ->
            let result = supportsColorWith (envOf []) (fun () -> false)
            Expect.isTrue result "Colors should be supported by default"

        testCase "NO_COLOR set -> not supported" <| fun _ ->
            let result = supportsColorWith (envOf ["NO_COLOR", "1"]) (fun () -> false)
            Expect.isFalse result "NO_COLOR should disable colors"

        testCase "NO_COLOR set to empty string -> not disabled" <| fun _ ->
            let result = supportsColorWith (envOf ["NO_COLOR", ""]) (fun () -> false)
            Expect.isTrue result "An empty NO_COLOR should be treated as unset, per no-color.org"

        testCase "Output redirected -> not supported" <| fun _ ->
            let result = supportsColorWith (envOf []) (fun () -> true)
            Expect.isFalse result "Redirected output should disable colors"

        testCase "TERM=dumb -> not supported" <| fun _ ->
            let result = supportsColorWith (envOf ["TERM", "dumb"]) (fun () -> false)
            Expect.isFalse result "TERM=dumb should disable colors"

        testCase "TERM other value, not redirected, no NO_COLOR -> supported" <| fun _ ->
            let result = supportsColorWith (envOf ["TERM", "xterm-256color"]) (fun () -> false)
            Expect.isTrue result "A normal TERM value should not disable colors"

        testCase "FORCE_COLOR set, output redirected -> supported" <| fun _ ->
            let result = supportsColorWith (envOf ["FORCE_COLOR", "1"]) (fun () -> true)
            Expect.isTrue result "FORCE_COLOR should force colors even when redirected"

        testCase "FORCE_COLOR set, overrides NO_COLOR" <| fun _ ->
            let result = supportsColorWith (envOf ["FORCE_COLOR", "1"; "NO_COLOR", "1"]) (fun () -> false)
            Expect.isTrue result "FORCE_COLOR should take precedence over NO_COLOR"

        testCase "FORCE_COLOR set to empty string -> not forced" <| fun _ ->
            let result = supportsColorWith (envOf ["FORCE_COLOR", ""; "NO_COLOR", "1"]) (fun () -> false)
            Expect.isFalse result "An empty FORCE_COLOR should be treated as unset"
    ]
