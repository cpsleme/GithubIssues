namespace GitHubIssues

open Infra
open System.Collections.Generic
open System.Net
open System.Diagnostics
open System.Text.Json
open System
open Domain

// think about IO but not its implementation
module App =

    // Generic function to get some data
    let getData info urlBase token =
        let issuesURI = $"{urlBase}/{info}"
        let stopWatch = Stopwatch.StartNew()

        let response = task { return (Infra.get issuesURI token) }

        stopWatch.Stop()

        response.Result, stopWatch.Elapsed.Milliseconds

    // Get issues from repo using Github REST API
    let getIssues urlBase token =

        let issuesJson, timeElapsed = getData "issues" urlBase token

        try
            let issuesResult = JsonSerializer.Deserialize<IssueList> issuesJson

            let issues: IssueResponse list =
                issuesResult
                |> List.map (fun x ->
                    { Title = x.title
                      Author = x.user.login
                      Labels = x.labels |> List.choose (fun l -> Some l.name) })

            printfn
                $"{DateTime.Now.ToString()} - Issues: {issuesResult.Length} Time elapsed: {timeElapsed:N0} milliseconds."

            let result = { Issues = issues; Error = None }
            result
        with ex ->
            let error = JsonSerializer.Deserialize<Error> issuesJson
            let result = { Issues = []; Error = Some error }
            result


    // Get commits from repo using Github REST API
    let getCommits urlBase token =

        let commitsJson, timeElapsed = getData "commits" urlBase token

        try
            let commitsResult = JsonSerializer.Deserialize<CommitList> commitsJson

            printfn
                $"{DateTime.Now.ToString()} - Commits: {commitsResult.Length} Time elapsed: {timeElapsed:N0} milliseconds."

            let commitsByUser =
                commitsResult
                |> List.countBy (fun x -> x.commit.author.name, x.author.login)
                |> List.map (fun ((name, user), qty) ->
                    { Name = name
                      User = user
                      QtdCommits = qty })

            let result =
                { Commits = commitsByUser
                  Error = None }

            result
        with ex ->
            let error = JsonSerializer.Deserialize<Error> commitsJson
            let result = { Commits = []; Error = Some error }
            result

    // Send to webhook
    let sendToWebhook resultJson webhook =
        let response = task { return Infra.post webhook resultJson }

        match response.Result.statusCode with
        | HttpStatusCode.OK -> printfn $"{DateTime.Now.ToString()} - Result sent to webhook."
        | _ -> printfn $"{DateTime.Now.ToString()} - Error: {response.Result.reasonPhrase}"

    let run (config: IDictionary<string, string>) =
        try
            // Config variables
            let username = config["GITHUB_USERNAME"]
            let repo = config["GITHUB_REPO"]
            let token = config["GITHUB_API_TOKEN"]
            let webhook = config["WEBHOOK_URL"]

            // Set URL Base
            let urlBase = $"https://api.github.com/repos/{username}/{repo}"

            // Get issues
            let issuesResult = getIssues urlBase token

            let issuesError =
                match issuesResult.Error with
                | Some x ->
                    printfn $"{DateTime.Now.ToString()} - Error: {x.message} Documentation: {x.documentation_url}"
                    issuesResult.Error
                | _ -> None

            // Get commits
            let commitsByUserResult = getCommits urlBase token

            let commitsError =
                match commitsByUserResult.Error with
                | Some x ->
                    printfn $"{DateTime.Now.ToString()} - Error: {x.message} Documentation: {x.documentation_url}"
                    commitsByUserResult.Error
                | _ -> None

            // Generate list of results
            let resultJson =
                match (issuesError, commitsError) with
                | None, None ->

                    let result =
                        { User = username
                          Repository = repo
                          Issues = issuesResult.Issues
                          Contributors = commitsByUserResult.Commits }

                    JsonSerializer.Serialize result

                | _, _ ->
                    let result =
                        { Issues = issuesError
                          Commits = commitsError }

                    JsonSerializer.Serialize result

            printfn $"{DateTime.Now.ToString()} - Result: {resultJson}"

            // Send to webhook
            sendToWebhook resultJson webhook
            ()

        with ex ->
            printfn $"{DateTime.Now.ToString()} - Error: {ex.Message}"

        ()
