# dotgraphee

dotnet + GraphQl + EntityFramework, Easy!

dotgraphee is a generator to kick-start your dotnet graphQL webserver projects.

You do:
 - Modify default configuration file to your liking.
 - Specify your (initial) data model.
 - Run generator.

You get:
 - Dotnet web-service with Hot-Chocolate GraphQL endpoint, serving your data model using queries, mutations, and subscriptions.
 - EntityFramework, wired up and configured to use a Postgres instance.
 - Docker file + docker-compose file, ready to go.
 - Integration-level automated tests for said docker container.
 - All code generated with dotnet null-safety enabled.

## How to use

To do!

## Development of dotgraphee

GenAndTest runs dotgraphee with its default configuration, then it executes the container tests.
Running this script takes several minutes, but should result in an all-tests-passed message.

### How to pack as dotnet tool
 - 'dotnet build'
 - 'dotnet pack'

### Install / Run / Uninstall (Only for development of dotgraphee!)
 - dotnet tool install --global --add-source ./nupkg dotgraphee
 - dotgraphee
 - dotnet tool uninstall --global dotgraphee

 ### Publish the tool

 https://docs.microsoft.com/en-us/nuget/quickstart/create-and-publish-a-package-using-the-dotnet-cli
