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

### Install the tool:

`dotnet tool install --global dotgraphee`

### Create the default configuration:

`dotgraphee`

File 'dotgraphee-config.json' will be created. Modify it to represent your own data model and preferred configuration.
If you don't know what to put, assume the default is fine.

### Generate your webservice:

`dotgraphee <config-file-here>`

It might take a few minutes to pull all the required packages. Afterwards, see the 'Readme.md' file in the newly created project folder.

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

'dotnet nuget push ".\nupkg\dotgraphee.x.x.x.nupkg" -k <secret api key here> -s https://www.nuget.org'

## Questions, Comments, Requests

Please see the Contributing.md
