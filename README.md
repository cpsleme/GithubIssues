# Project

This is a project that queries a github repository bringing information about issues and counting the number of commits per user.
This query is performed from time to time with an interval in hours informed at application startup.

# Development stack

The project was developed in the F# language (F Sharp) of the Microsoft .NET platform, and in this case follows the functional paradigm, since the language is multi-paradigm.

# Architecture

The project follows an organization in layers following an architectural pattern called Onion Architecture.

Reference: https://marcoatschaefer.medium.com/onion-architecture-explained-building-maintainable-software-54996ff8e464

# installation requirements

- .NET SDK, version 6 or higher
  https://dotnet.microsoft.com/en-us/download

# Execution requirements

- It is necessary to create an API Token on github:

  https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token

- Set environment variables:

  "GITHUB_USERNAME" -> Github User.
  
  "GITHUB_REPO" -> Repository for consultation.
  
  "GITHUB_API_TOKEN" -> API Token generated on Github.
  
  "WEBHOOK_URL" -> Target webhook.
  
  "CHECKING_INTERVAL" - Interval in hours that the query will be performed.

# To execute

  dotnet restore
  
  dotnet run

