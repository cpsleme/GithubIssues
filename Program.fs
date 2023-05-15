open System
open System.Reflection
open System.Threading
open FsHttp
open System.Text.Json
open System.Diagnostics
open System.Timers

// Data structs, don't think about IO at all
module Domain =

    // Response from Github
    type User = { id: int64; login: string }

    type Label = { id: int64; name: string }

    type Issue =
        { title: string
          user: User
          labels: Label list }

    type IssueList = Issue list

    type Author = { login: string }

    type AuthorInfo = { name: string }

    type CommitInfo = { author: AuthorInfo }

    type Commit = { commit: CommitInfo; author: Author }

    type CommitList = Commit list

    // Response to webhook
    type IssueResponse =
        { Title: string
          Author: string
          Labels: string list }

    type Contributor =
        { Name: string
          User: string
          QtdCommits: int }

    type Repo =
        { User: string
          Repository: string
          Issues: IssueResponse list
          Contributors: Contributor list }

// IO implementation
module Infra =
    let get uri token =
        http {
            GET uri
            Accept "application/vnd.github+json"
            Authorization $"Bearer {token}"
            UserAgent "Awesome-Octocat-App"
        }
        |> Request.send
        |> Response.toJson

    let post uri sendData =
        http {
            POST uri
            CacheControl "no-cache"
            body
            json sendData
        }
        |> Request.send

// think about IO but not its implementation
module App =
    open Domain

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
        let issuesResult = JsonSerializer.Deserialize<IssueList> issuesJson

        printfn
            $"{DateTime.Now.ToString()} - Issues: {issuesResult.Length} Time elapsed: {timeElapsed:N0} milliseconds."

        let issues: IssueResponse list =
            issuesResult
            |> List.map (fun x ->
                { Title = x.title
                  Author = x.user.login
                  Labels = x.labels |> List.choose (fun l -> Some l.name) })

        issues

    // Get commits from repo using Github REST API
    let getCommits urlBase token =

        let commitsJson, timeElapsed = getData "commits" urlBase token
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

        commitsByUser

    let run username repo urlBase token webhook =
        try
            // Get issues
            let issues = getIssues urlBase token
            // Get commits
            let commitsByUser = getCommits urlBase token
            // Generate list of results
            let result =
                { User = username
                  Repository = repo
                  Issues = issues
                  Contributors = commitsByUser }

            // Serialize results
            let resultJson = JsonSerializer.Serialize result
            printfn $"{DateTime.Now.ToString()} - Result: {resultJson}"

            // Send to webhook
            let response =
                task { return Infra.post webhook resultJson }

            match response.Result.statusCode with
            | OK -> printfn $"{DateTime.Now.ToString()} - Result sent to webhook."
            | _ -> printfn $"{DateTime.Now.ToString()} - Error: {response.Result.reasonPhrase}"

            ()

        with ex ->
            printfn $"{DateTime.Now.ToString()} - Error: {ex.Message}"

        ()

// Startup app with scheduler
module Startup =
    let run username repo token checkingInterval webhook =

        // Set URL Base
        let urlBase = $"https://api.github.com/repos/{username}/{repo}"

        // Start a timer
        let interval = checkingInterval * 1000 * 60 * 60
        let aTimer = new Timer(interval)
        aTimer.Elapsed.Add(fun _ -> App.run username repo urlBase token webhook)
        aTimer.Enabled <- true

        // Everything ok to start
        printfn $"{DateTime.Now.ToString()} - Getting issues from {repo}"

        // Exec first time
        App.run username repo urlBase token webhook

        // Infinite loop to keep alive
        while true do
            Thread.Sleep(120000)
            printfn $"{DateTime.Now.ToString()} - Still alive."

[<EntryPoint>]
let main argv =

    let build = Assembly.GetExecutingAssembly().GetName().Version
    printfn $"Github Issues - Version: {build}"
    printfn ""

    // Get environment variables
    let username = Environment.GetEnvironmentVariable("GITHUB_USERNAME")
    match username with
    | null ->
        printfn "Please, set GITHUB_USERNAME environment variable."
        exit 1
    | _ -> ()

    let repo = Environment.GetEnvironmentVariable("GITHUB_REPO")
    match repo with
    | null ->
        printfn "Please, set GITHUB_REPO environment variable."
        exit 1
    | _ -> ()

    let token = Environment.GetEnvironmentVariable("GITHUB_API_TOKEN")
    match token with
    | null ->
        printfn "Please, set GITHUB_API_TOKEN environment variable."
        exit 1
    | _ -> ()

    let webhook = Environment.GetEnvironmentVariable("WEBHOOK_URL")
    match webhook with
    | null ->
        printfn "Please, set WEBHOOK_URL environment variable."
        exit 1
    | _ -> ()

    let checkingInterval =
        Int32.TryParse(Environment.GetEnvironmentVariable("CHECKING_INTERVAL"))

    let interval =
        match checkingInterval with
        | true, int -> int
        | _ -> 24

    printfn $"{DateTime.Now.ToString()} - App ready to run."
    Startup.run username repo token  interval webhook

    0
