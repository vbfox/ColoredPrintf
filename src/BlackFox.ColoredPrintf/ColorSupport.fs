module internal BlackFox.ColoredPrintf.ColorSupport

open System

/// Testable core: takes environment-variable lookup and redirection-check as
/// parameters so tests can supply fakes instead of touching real process state.
let supportsColorWith
    (getEnvironmentVariable: string -> string)
    (isOutputRedirected: unit -> bool) =

    // https://force-color.org : presence of a non-empty value forces color on, overriding
    // everything else (including NO_COLOR)
    let hasForceColor = not (String.IsNullOrEmpty(getEnvironmentVariable "FORCE_COLOR"))

    // https://no-color.org : presence of a non-empty value disables color, regardless of
    // its content
    let hasNoColor = not (String.IsNullOrEmpty(getEnvironmentVariable "NO_COLOR"))

    if hasForceColor then true
    elif hasNoColor then false
    elif isOutputRedirected () then false
    elif getEnvironmentVariable "TERM" = "dumb" then false
    else true

/// Detect if the current process should output colors: true when FORCE_COLOR is set
/// (overriding everything else); otherwise false when NO_COLOR is set, when standard
/// output is redirected, or when TERM=dumb; true otherwise.
let supportsColor () =
    supportsColorWith Environment.GetEnvironmentVariable (fun () -> Console.IsOutputRedirected)
