# pkgchk-cli

[![Build & Release](https://github.com/tonycknight/pkgchk-cli/actions/workflows/build.yml/badge.svg)](https://github.com/tonycknight/pkgchk-cli/actions/workflows/build.yml)

![Nuget](https://img.shields.io/nuget/v/pkgchk-cli)

A dotnet tool for package dependency checks.

`dotnet list package` is a wonderful tool, and with its `--vulnerable` switch it is essential for code provenance. If you're not famlilar with it or why it's recommended, [see this blog post](https://devblogs.microsoft.com/nuget/how-to-scan-nuget-packages-for-security-vulnerabilities/).

Unfortunately, simple integration into CI pipelines isn't feasible: the tool does not return a non-zero return code when vulnerabilities are found; and CI pipelines rely on return codes. Users are left to parse the tool's console output, and so must maintain different scripts for different environments.

There are long-lived issues on the Dotnet & Nuget boards, which seem to be stuck:
- [Dotnet issue 16852](https://github.com/dotnet/sdk/issues/16852)
- [Dotnet issue 25091](https://github.com/dotnet/sdk/issues/25091)
- [Nuget issue 11781](https://github.com/NuGet/Home/issues/11781)

So until those issues are resolved, `dotnet list package` needs some workarounds in CI pipelines.

This tool wraps `dotnet list package` and interprets the output for vulnerabilities. Anything found will return in a non-zero return code. CI integration is as easy as local use.

## Installation requirements

:warning: This tool only works with .Net SDK 7.0.200 or higher. 

You'll need .Net SDK 7.0.200 installed. Any `global.json` files must use .Net SDK 7.0.200 or higher.

If your effective SDK is lower than 7.0.200, this tool will work with unexpected results.

## Installation into your repository

Create a tool manifest for your reepository:

```dotnet new tool-manifest```

Add the tool to your repository's toolset:

```dotnet tool install pkgchk-cli```

## Use

To check for top-level dependency vulnerabilities:

```pkgchk <project|solution>```

To check for top-level and transitive dependency vulnerabilities:

```pkgchk <project|solution> --transitive```

If there's only one project or solution file in your directory, omit the `<project|solution>` argument.


## Integration within Github actions

Simply:

```
name: run SCA
runs: |
    dotnet tool restore
    pkgchk <project|solution> --transitive
```

## Integration within other CI platforms

Most CI platforms fail on non-zero return codes from steps. 

Simply ensure your repository has `pkgchk-cli` in its tools manifest, your CI includes `nuget.org` as a package source and run:

```
dotnet tool restore
pkgchk <project|solution> --transitive
```


## Licence

`pkgchk-cli` is licenced under MIT.

`pkgchk-cli` uses [Spectre.Console](https://spectreconsole.net/) - please check their licence.

`pkgchk-cli` uses [`dotnet list package`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-list-package) published by Microsoft.