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
COPY ["src/Plugins/Solutions/DevTools/DevTools.csproj", "src/Plugins/Solutions/DevTools/"]
COPY ["src/Plugins/Integrations/AuthKit.Plugins.Integrations.csproj", "src/Plugins/Integrations/"]

COPY ["src/Plugins/Solutions/ExamplePlugin/ExamplePlugin.csproj", "src/Plugins/Solutions/ExamplePlugin/"]

COPY ["tests/Host/AuthKit.Host.Tests.csproj", "tests/Host/"]
COPY ["tests/Host.IntegrationTests/AuthKit.Host.IntegrationTests.csproj", "tests/Host.IntegrationTests/"]
COPY ["tests/Plugins/Abstractions/AuthKit.Plugins.Abstractions.Tests.csproj", "tests/Plugins/Abstractions/"]
COPY ["tools/AuthKit.PluginContractValidator/AuthKit.PluginContractValidator.csproj", "tools/AuthKit.PluginContractValidator/"]

RUN dotnet restore "AuthKit.slnx"

COPY . .
RUN mkdir /root/certs

WORKDIR "/src"
RUN dotnet build "src/Host/Host.csproj" -c Release -o /app/build

FROM node:22 AS ui
WORKDIR /ui
COPY ["src/Plugins/Solutions/DevTools/UI/package.json", "src/Plugins/Solutions/DevTools/UI/package-lock.json", "./"]
RUN npm ci
COPY ["src/Plugins/Solutions/DevTools/UI/template.html", "src/Plugins/Solutions/DevTools/UI/vite.config.ts", "./"]
COPY ["src/Plugins/Solutions/DevTools/UI/src/", "./src/"]
RUN npm run build

FROM build AS publish
WORKDIR /src
RUN dotnet publish "src/Host/Host.csproj" -c Release -o /app/publish
RUN dotnet publish "src/Plugins/Solutions/DevTokens/DevTokens.csproj" -c Release -o /app/publish/plugins/DevTokens
COPY src/Plugins/Solutions/DevTokens/manifest.json /app/publish/plugins/DevTokens/manifest.json

COPY --from=ui /ui/dist/ui.html src/Plugins/Solutions/DevTools/UI/dist/ui.html
RUN dotnet publish "src/Plugins/Solutions/DevTools/DevTools.csproj" -c Release -o /app/publish/plugins/DevTools
COPY src/Plugins/Solutions/DevTools/manifest.json /app/publish/plugins/DevTools/manifest.json

RUN dotnet publish "src/Plugins/Solutions/ExamplePlugin/ExamplePlugin.csproj" -c Release -o /app/publish/plugins/ExamplePlugin
COPY src/Plugins/Solutions/ExamplePlugin/manifest.json /app/publish/plugins/ExamplePlugin/manifest.json

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=publish /app/publish/plugins ./plugins
VOLUME /root/certs
ENTRYPOINT ["dotnet", "Host.dll"]
