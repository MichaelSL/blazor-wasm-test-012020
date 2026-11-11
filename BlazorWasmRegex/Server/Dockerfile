#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Overridden by CI: VERSION=yyyy.M.d.<run number>, SOURCE_REV=<short commit sha>.
ARG VERSION=0.0.0.1
ARG SOURCE_REV=local

FROM mcr.microsoft.com/dotnet/aspnet:11.0-resolute-chiseled-extra AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /src
COPY ["BlazorWasmRegex/Server/BlazorWasmRegex.Server.csproj", "BlazorWasmRegex/Server/"]
COPY ["BlazorWasmRegex/Client/BlazorWasmRegex.Client.csproj", "BlazorWasmRegex/Client/"]
COPY ["BlazorWasmRegex/Shared/BlazorWasmRegex.Shared.csproj", "BlazorWasmRegex/Shared/"]
COPY ["Directory.Build.props", ""]
RUN dotnet restore "BlazorWasmRegex/Server/BlazorWasmRegex.Server.csproj"
COPY . .
ARG VERSION
ARG SOURCE_REV
WORKDIR "/src/BlazorWasmRegex/Server"
# Stamp the published service worker with the commit so browsers pick up a new worker per deploy.
# This edits the container's copy of the source only.
RUN echo "// build: ${SOURCE_REV}" >> ../Client/wwwroot/service-worker.published.js
RUN dotnet build "BlazorWasmRegex.Server.csproj" -c Release -o /app/build -p:Version=${VERSION} -p:SourceRevisionId=${SOURCE_REV}

FROM build AS publish
ARG VERSION
ARG SOURCE_REV
RUN dotnet publish "BlazorWasmRegex.Server.csproj" -c Release -o /app/publish -p:Version=${VERSION} -p:SourceRevisionId=${SOURCE_REV}

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BlazorWasmRegex.Server.dll"]