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

open Domain

module Infra =
    let get (uri: string, token: string) =
        http {
            GET uri
            Accept "application/vnd.github+json"
            Authorization $"Bearer {token}"
            UserAgent "Awesome-Octocat-App"
        }
        |> Request.send
        |> Response.toJson

// think about IO but not its implementation
module App =
    open Domain

    let getIssues urlBase token =
        let issuesURI = $"{urlBase}/issues"
        let stopWatch = Stopwatch.StartNew()

        let requestIssues =
            task { return JsonSerializer.Deserialize<IssueList>(Infra.get (issuesURI, token)) }

        stopWatch.Stop()

        requestIssues.Result, stopWatch.Elapsed.Milliseconds

    let getCommits urlBase token =
        let commitsURI = $"{urlBase}/commits"
        let stopWatch = Stopwatch.StartNew()

        let requestCommits =
            task { return JsonSerializer.Deserialize<CommitList>(Infra.get (commitsURI, token)) }

        stopWatch.Stop()

        requestCommits.Result, stopWatch.Elapsed.Milliseconds

    let run (username: string, repo: string, urlBase: string, token: string) =
        try
            // Get issues from repo using Github REST API
            let issuesResult, timeElapsed = getIssues urlBase token

            printfn
                $"{DateTime.Now.ToString()} - Issues: {issuesResult.Length} Time elapsed: {timeElapsed:N0} milliseconds."

            let issues =
                issuesResult
                |> List.map (fun x ->
                    { Title = x.title
                      Author = x.user.login
                      Labels = x.labels |> List.choose (fun l -> Some l.name) })

            // Get commits from repo using Github REST API
            let commits, timeElapsed = getCommits urlBase token

            printfn
                $"{DateTime.Now.ToString()} - Commits: {commits.Length} Time elapsed: {timeElapsed:N0} milliseconds."

            let commitsByUser =
                commits
                |> List.countBy (fun x -> x.commit.author.name, x.author.login)
                |> List.map (fun ((name, user), qty) ->
                    { Name = name
                      User = user
                      QtdCommits = qty })

            // Generate list of results
            let result =
                { User = username
                  Repository = repo
                  Issues = issues
                  Contributors = commitsByUser }
                
            // Serialize results
            let resultJson = JsonSerializer.Serialize result
            
            printfn $"{resultJson}"
            ()

        with ex ->
            printfn $"Error: {ex.Message}"

        ()

// Startup app with scheduler
module Startup =
    let run (username: string, repo: string, token: string) =

        // Set URL Base
        let urlBase = $"https://api.github.com/repos/{username}/{repo}"

        // Start a timer
        let aTimer = new Timer(60000.0)
        aTimer.Elapsed.Add(fun _ -> App.run (username, repo, urlBase, token))
        aTimer.Enabled <- true

        // Everything ok to start
        printfn $"{DateTime.Now.ToString()} - Getting issues from {repo}"
        App.run (username, repo, urlBase, token)

        // Infinite loop to keep alive
        while true do
            Thread.Sleep(120000)
            printfn $"{DateTime.Now.ToString()} - Still alive."

[<EntryPoint>]
let main argv =

    let build = Assembly.GetExecutingAssembly().GetName().Version
    printfn $"Github Issues - Version: {build}"
    printfn ""

    // Get user inputs or environment variables
    let username, repo, config =
        match argv.Length with
        | 2 -> argv[0], argv[1], "input"
        | _ ->
            Environment.GetEnvironmentVariable("GITHUB_USERNAME"),
            Environment.GetEnvironmentVariable("GITHUB_REPO"),
            "env"

    if config.Equals("env") then
        match username with
        | null ->
            printfn "Please, set GITHUB_USERNAME environment variable."
            exit 1
        | _ -> ()

        match repo with
        | null ->
            printfn "Please, set GITHUB_REPO environment variable."
            exit 1
        | _ -> ()

    // Get Github token
    let token = Environment.GetEnvironmentVariable("GITHUB_API_TOKEN")

    match token with
    | null ->
        printfn "Please, set GITHUB_API_TOKEN environment variable."
        exit 1
    | _ -> printfn $"{DateTime.Now.ToString()} - App ready to run."

    Startup.run (username, repo, token)

    0
