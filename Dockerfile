FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["AuthKit.slnx", "."]
COPY ["Directory.Packages.props", "."]

COPY ["src/Host/Host.csproj", "src/Host/"]
COPY ["src/Core/Core.csproj", "src/Core/"]
COPY ["src/Plugins/Abstractions/AuthKit.Plugins.Abstractions.csproj", "src/Plugins/Abstractions/"]
COPY ["src/Plugins/Solutions/DevTokens/DevTokens.csproj", "src/Plugins/Solutions/DevTokens/"]
COPY ["tools/PluginContractValidator/PluginContractValidator.csproj", "tools/PluginContractValidator/"]

RUN dotnet restore "AuthKit.slnx"

COPY . .
RUN mkdir /root/certs

WORKDIR "/src"
RUN dotnet build "src/Host/Host.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR /src
RUN dotnet publish "src/Host/Host.csproj" -c Release -o /app/publish
RUN dotnet publish "src/Plugins/Solutions/DevTokens/DevTokens.csproj" -c Release -o /app/publish/plugins/DevTokens
COPY src/Plugins/Solutions/DevTokens/plugin.manifest.json /app/publish/plugins/DevTokens/plugin.manifest.json

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=publish /app/publish/plugins ./plugins
VOLUME /root/certs
ENTRYPOINT ["dotnet", "Host.dll"]
