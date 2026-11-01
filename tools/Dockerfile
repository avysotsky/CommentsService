FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY global.json Directory.Build.props ./
COPY src/CommentsService.Domain/ ./src/CommentsService.Domain/
COPY src/CommentsService.Application/ ./src/CommentsService.Application/
COPY src/CommentsService.Infrastructure/ ./src/CommentsService.Infrastructure/
COPY tools/CommentsService.Tools/ ./tools/CommentsService.Tools/
RUN dotnet publish tools/CommentsService.Tools -c Release -o /out /p:UseAppHost=false

# Official PostgreSQL17 clients plus .NET10 SDK for optional containerized backend tests.
# No Docker CLI/socket and no Python interpreter is used by the tool.
FROM postgres:17.11-bookworm
COPY --from=build /usr/share/dotnet/ /usr/share/dotnet/
COPY --from=build /etc/ssl/certs/ca-certificates.crt /etc/ssl/certs/ca-certificates.crt
COPY --from=build /out/ /tools/
ENV HOME=/tmp/dotnet-home DOTNET_ROOT=/usr/share/dotnet PATH=/usr/share/dotnet:$PATH DOTNET_CLI_HOME=/tmp/dotnet-home NUGET_PACKAGES=/tmp/nuget DOTNET_NOLOGO=true DOTNET_CLI_TELEMETRY_OPTOUT=true
WORKDIR /workspace
ENTRYPOINT ["dotnet", "/tools/CommentsService.Tools.dll"]
