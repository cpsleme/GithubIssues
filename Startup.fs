namespace GitHubIssues

open System
open System.Collections.Generic
open System.Threading
open System.Timers

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
        aTimer.Elapsed.Add(fun _ -> App.run config)
        aTimer.Enabled <- true

        // Everything ok to start
        printfn $"{DateTime.Now.ToString()} - Getting issues and commits from {repo} repository."

        // Exec first time
        App.run config

        // Infinite loop to keep alive
        while true do
            Thread.Sleep(120000)
            printfn $"{DateTime.Now.ToString()} - Still alive."