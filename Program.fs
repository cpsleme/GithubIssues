open System.Collections.Generic
open System.Reflection
open System.Threading
open System.Timers
open System

module Startup =
    let run (config: IDictionary<string, string>) =

        printfn $"{DateTime.Now.ToString()} - App ready to run."

        // Config variables
        let repo = config["GITHUB_REPO"]
        let intervalEnv =
            match Int32.TryParse(config["CHECKING_INTERVAL"]) with
            | true, int -> int
            | _ -> 1

        // Start a timer
        let interval = intervalEnv * 1000 * 60 * 60
        let aTimer = new Timer(interval)
        aTimer.Elapsed.Add(fun _ -> GitHubIssues.App.run config)
        aTimer.Enabled <- true

        // Everything ok to start
        printfn $"{DateTime.Now.ToString()} - Getting issues and commits from {repo} repository."

        // Exec first time
        GitHubIssues.App.run config

        // Infinite loop to keep alive
        while true do
            Thread.Sleep(120000)
            printfn $"{DateTime.Now.ToString()} - Still alive."


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
