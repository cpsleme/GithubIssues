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
    type User =
        { login: string
          id: int
          node_id: string
          avatar_url: string
          gravatar_id: string
          url: string
          html_url: string
          followers_url: string
          following_url: string
          gists_url: string
          starred_url: string
          subscriptions_url: string
          organizations_url: string
          repos_url: string
          events_url: string
          received_events_url: string
          user_type: string
          site_admin: bool }

    type Reactions =
        { url: string
          total_count: int
          //"+1": int
          //"-1": int
          laugh: int
          hooray: int
          confused: int
          heart: int
          rocket: int
          eyes: int }

    type Issue =
        { url: string
          repository_url: string
          labels_url: string
          comments_url: string
          events_url: string
          html_url: string
          id: int
          node_id: string
          number: int
          title: string
          user: User
          labels: string list
          state: string
          locked: bool
          assignee: User option
          assignees: User list
          milestone: unit option
          comments: int
          created_at: string
          updated_at: string
          closed_at: string option
          author_association: string
          active_lock_reason: string option
          body: string
          reactions: Reactions
          timeline_url: string
          performed_via_github_app: unit option
          state_reason: unit option }

    type IssueList = Issue list

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
    let Get (uri: string, token: string) =
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
            task { return JsonSerializer.Deserialize<IssueList>(Infra.Get(issuesURI, token)) }

        stopWatch.Stop()

        requestIssues.Result.Length, stopWatch.Elapsed.Milliseconds

    let run (urlBase: string, token: string) =
        try
            // Get issues from repo using Github REST API
            let issuesQuantity, timeElapsed = getIssues urlBase token
            printfn $"{DateTime.Now.ToString()} - Issues: {issuesQuantity} Time elapsed: {timeElapsed:N0} milliseconds."

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
        aTimer.Elapsed.Add(fun _ -> App.run (urlBase, token))
        aTimer.Enabled <- true

        // Everything ok to start
        printfn $"Getting issues from {repo}"

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
    | _ -> printfn "App ready to run."

    Startup.run (username, repo, token)

    0

