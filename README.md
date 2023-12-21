# pkgchk-cli

A dotnet tool for package dependency checks.

`dotnet list package` is a wonderful tool, and with its `--vulnerable` switch it is essential for code provenance. 

Unfortunately, simple integration into CI pipelines isn't feasible: the tool does not return a non-zero return code when vulnerabilities are found; and CI pipelines rely on return codes. Users are left to parse the tool's console output, and so must maintain different scripts for different environments.

This tool is a wrapper around `dotnet list package` and interprets the output for vulnerabilities. Anything found will return in a non-zero return code. CI integration is as easy as local use.

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


## Integration within Github actions

Simply:

```dotnet tool restore```

```pkgchk <project|solution> --transitive```

## Licence

`pkgchk-cli` is licenced under MIT.

`pkgchk-cli` uses [Spectre.Console](https://spectreconsole.net/) - please check their licence.

`pkgchk-cli` uses [`dotnet list package`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-list-package) published by Microsoft.