FROM node:24-alpine AS frontend
WORKDIR /web
COPY src/comments-web/package.json src/comments-web/package-lock.json ./
RUN npm ci --no-fund --no-audit
COPY src/comments-web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /source
COPY global.json Directory.Build.props ./
COPY src/CommentsService.Api/CommentsService.Api.csproj ./src/CommentsService.Api/
RUN dotnet restore src/CommentsService.Api
COPY src/CommentsService.Api/ ./src/CommentsService.Api/
RUN dotnet publish src/CommentsService.Api -c Release --no-restore -o /out /p:UseAppHost=false
COPY --from=frontend /web/dist/ /out/wwwroot/

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && apt-get clean
WORKDIR /app
COPY --from=backend /out/ ./
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
HEALTHCHECK --interval=5s --timeout=3s --start-period=20s --retries=10 CMD ["dotnet", "CommentsService.Api.dll", "--healthcheck"]
ENTRYPOINT ["dotnet", "CommentsService.Api.dll"]
