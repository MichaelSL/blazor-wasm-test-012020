#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build
WORKDIR /src
COPY ["BlazorWasmRegex/Server/BlazorWasmRegex.Server.csproj", "BlazorWasmRegex/Server/"]
COPY ["BlazorWasmRegex/Client/BlazorWasmRegex.Client.csproj", "BlazorWasmRegex/Client/"]
COPY ["BlazorWasmRegex/Shared/BlazorWasmRegex.Shared.csproj", "BlazorWasmRegex/Shared/"]
COPY ["Directory.Build.props", ""]
RUN dotnet restore "BlazorWasmRegex/Server/BlazorWasmRegex.Server.csproj"
COPY . .
WORKDIR "/src/BlazorWasmRegex/Server"
RUN dotnet build "BlazorWasmRegex.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BlazorWasmRegex.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BlazorWasmRegex.Server.dll"]