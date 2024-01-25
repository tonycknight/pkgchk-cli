ARG BuildVersion

FROM mcr.microsoft.com/dotnet/sdk:7.0-jammy AS base
WORKDIR /app


# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN adduser -u 5678 --disabled-password --gecos "" pkgchkuser && chown -R pkgchkuser /app
USER pkgchkuser

FROM mcr.microsoft.com/dotnet/sdk:7.0-jammy AS build
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
RUN dotnet publish "pkgchk-cli.fsproj" -c Release -o /app/publish /p:AssemblyInformationalVersion=${BuildVersion} /p:AssemblyFileVersion=${BuildVersion} 

LABEL org.opencontainers.image.source https://github.com/tonycknight/pkgchk-cli

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "/pkgchk-cli.dll"]
