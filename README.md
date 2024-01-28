# pkgchk-cli

[![Build & Release](https://github.com/tonycknight/pkgchk-cli/actions/workflows/build.yml/badge.svg)](https://github.com/tonycknight/pkgchk-cli/actions/workflows/build.yml)

[![Nuget](https://img.shields.io/nuget/v/pkgchk-cli)](https://www.nuget.org/packages/pkgchk-cli/)

A dotnet tool for package dependency checks.

`dotnet list package` is a wonderful tool and with its `--vulnerable` option it is essential for verifying your project's dependencies. It's quick, easy and _free_. If you're not famlilar with it or why you should depend on it (pun intented), [read this blog post](https://devblogs.microsoft.com/nuget/how-to-scan-nuget-packages-for-security-vulnerabilities/).

Unfortunately, integrating it into your CI pipelines isn't as simple as you'd hope: the tool does not return a non-zero return code when vulnerabilities are found (what _every_ pipeline needs), and doesn't produce any reports for things like PR checks. We're left to dig into the build logs and parse the tool's console output to see what's up.

There are long-lived issues on the Dotnet & Nuget boards:
- [Dotnet issue 16852](https://github.com/dotnet/sdk/issues/16852)
- [Dotnet issue 25091](https://github.com/dotnet/sdk/issues/25091)
- [Nuget issue 11781](https://github.com/NuGet/Home/issues/11781)

So until those issues are resolved, `dotnet list package` needs some workarounds in CI pipelines.

This tool tries to do just that. It wraps `dotnet list package` and interprets the output for vulnerabilities. Anything found will return in a non-zero return code, and you get some nice markdown to make your PRs obvious. And because it's a `dotnet tool`, using it in a CI pipeline is as easy as using it on your dev machine.

## If you want to use this as a Github Action

A Github Action is available - see [pkgchk-action](https://github.com/tonycknight/pkgchk-action).

## What you need to install it

:warning: This tool only works with .Net SDK 7.0.200 or higher. 

You'll need .Net SDK 7.0.200 installed. Any `global.json` files must use .Net SDK 7.0.200 or higher.

If your SDK is lower than 7.0.200, this tool will not work: you'll get some unexpected results. Sorry about that.
.Net 7.0.200 introduced JSON output, which `pkgchk-cli` leans on.

## Installing into your repository

If you want it in your pipelines, you'll need to install a version into your repository.

Create a tool manifest for your repository:

```dotnet new tool-manifest```

Add the tool to your repository's toolset:

```dotnet tool install pkgchk-cli```

## Installing onto your machine

If you want to use it _in every directory_ just add the tool to your global toolset:

```dotnet tool install pkgchk-cli -g```

## How to use it

To get help:

```pkgchk --help```

To check for top-level and transitive dependency vulnerabilities:

```pkgchk <project|solution>```

If there's only one project or solution file in your directory, omit the `<project|solution>` argument.

### Options

The following options are disjunctive: they can be used independently of each other, or all together as you like.

|  |  |  |   |
| - | - | - | - |
| `--vulnerable` | Scan for vulnerable packages | `true`/`false` | `true` by default |
| `--deprecated` | Scan for deprecated packages | `true`/`false` | `false` by default |
| `--dependencies` | Scan for dependency packages | `true`/`false` | `false` by default |
| `--transitive` | Scan for transitive packages, vulnerable, deprecated or otherwise | `true`/`false` | `true` by default |

Other options are:

|  |  |  |   |
| - | - | - | - |
| `--output` | The relative or absolute directory for reports. If ommitted, no reports are generated | `string` | None by default |
| `--severity` | Severity levels to search for, or deprecation reasons. Any number of severties can be given. | `string` | `High`, `Critical`, `Critical Bugs`, `Legacy` |
| `--no-restore` | Don't automatically restore the project/solution. | n/a | Package restoration is automatic by default |

### Examples


To check only for top-level dependency vulnerabilities:

```pkgchk <project|solution> --transitive false```

To add deprecated packages in a scan:

```pkgchk <project|solution> --deprecated```

Vulnerable packages are automatically searched for. To turn off vulnerable package searches::

```pkgchk <project|solution> --vulnerable false```

To list top-level dependencies with transitives:

```pkgchk <project|solution> --dependencies```

To list top-level dependencies without transitives:

```pkgchk <project|solution> --dependencies --transitive false```

To list dependencies only without any vulnerability checks:

```pkgchk <project|solution> --dependencies true --vulnerable false --deprecated false```

To produce a markdown file, simply give an output folder:

```pkgchk <project|solution> --output ./reports_directory```

Project restores (`dotnet restore`) occur automatically. To suppress restores, just add `--no-restore`:

```pkgchk <project|solution> --no-restore```

By default only `High`, `Critical`, `Critical Bugs` and `Legacy` vulnerabilities and deprecations are detected. Specify the vulnerability severities (or deprecation reasons) with ``--severity`` switches, e.g. to just check for `Moderate` issues:

```pkgchk <project|solution> --severity Moderate```

## Integration within Github actions

Simply:

```
name: run SCA
run: |
    dotnet tool restore    
    pkgchk <project|solution>
```

## Integration within other CI platforms

Most CI platforms fail on non-zero return codes from steps. 

Simply ensure your repository has `pkgchk-cli` in its tools manifest, your CI includes `nuget.org` as a package source and run:

```
dotnet tool restore
pkgchk <project|solution>
```


## Licence

`pkgchk-cli` is licenced under MIT.

`pkgchk-cli` uses [Spectre.Console](https://spectreconsole.net/) - please check their licence.

`pkgchk-cli` uses [`dotnet list package`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-list-package) published by Microsoft.