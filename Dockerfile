ARG BuildVersion

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base
ARG BuildVersion
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BuildVersion
WORKDIR /src

COPY ["src/pkgchk-cli/pkgchk-cli.fsproj", "src/pkgchk-cli/"]
RUN dotnet restore "src/pkgchk-cli/pkgchk-cli.fsproj"
COPY . .
WORKDIR "/src/src/pkgchk-cli"
RUN dotnet tool restore
RUN dotnet restore
RUN dotnet build "pkgchk-cli.fsproj" -c Release -o /app/build /p:AssemblyInformationalVersion=${BuildVersion} /p:AssemblyFileVersion=${BuildVersion} /p:NuGetVersion=${BuildVersion}

FROM build AS publish
ARG BuildVersion
RUN dotnet publish "pkgchk-cli.fsproj" -c Release -o /app/publish /p:AssemblyInformationalVersion=${BuildVersion} /p:Version=${BuildVersion} --os linux --arch x64 --self-contained

LABEL org.opencontainers.image.source https://github.com/tonycknight/pkgchk-cli

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "/app/pkgchk-cli.dll"]
