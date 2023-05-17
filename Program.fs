open System.Reflection
open System
open GitHubIssues

[<EntryPoint>]
let main =
    let build = Assembly.GetExecutingAssembly().GetName().Version
    printfn $"Github Issues - Version: {build}"
    printfn ""

    let envVariables =
        [ "GITHUB_USERNAME"
          "GITHUB_REPO"
          "GITHUB_API_TOKEN"
          "WEBHOOK_URL"
          "CHECKING_INTERVAL" ]

    let envVarValues =
        envVariables |> List.map (fun x -> x, Environment.GetEnvironmentVariable x)

    let envVarEmpty =
        envVarValues |> List.filter (fun (_, value) -> value.Equals(String.Empty))

    let errorCode =
        match envVarEmpty.Length with
        | 0 ->
            let config = envVarValues |> dict
            Startup.run config
            0
        | _ ->
            envVarEmpty
            |> List.iter (fun (var, _) -> printfn $"Please, set {var} environment variable.")
            1

    exit errorCode
