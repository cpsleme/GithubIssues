namespace GitHubIssues

open FsHttp

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
