namespace GitHubIssues

// Data structs, don't think about IO at all
module Domain =

    // Response from Github
    type Error =
        { message: string
          documentation_url: string }

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

    type IssueResult =
        { Issues: IssueResponse list
          Error: Error option }

    type CommitResult =
        { Commits: Contributor list
          Error: Error option }

    type ErrorResult =
        { Issues: Error option
          Commits: Error option }
