# setup build environment
FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS build-env
WORKDIR /app

# copy everything and build the project
COPY src nuget.config ./
# remove sourcelink to avoid warning when not copyting root dir with along .git 
RUN sed -i -e '/Microsoft.SourceLink.GitHub/d' Directory.Build.props
RUN sed -i -e '/Nerdbank.GitVersioning/d' Directory.Build.props
WORKDIR /app/Opserver.Poller
RUN dotnet restore
RUN dotnet publish -c Release -o out --no-restore

# build runtime image
# FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 is not able to connect to mssql 2014
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine
WORKDIR /app
COPY --from=build-env /app/Opserver.Poller/out ./

ENTRYPOINT ["dotnet", "Opserver.Poller.dll"]
