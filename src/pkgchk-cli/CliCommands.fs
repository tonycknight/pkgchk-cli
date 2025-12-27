namespace pkgchk

type GithubContext =
    { token: string
      repo: string
      summaryTitle: string
      prId: string
      commit: string }

type ReportContext =
    { reportDirectory: string
      goodImageUri: string 
      baddImageUri: string }

type OptionsContext =
    { projectPath: string
      suppressBanner: bool
      suppressRestore: bool
      includedPackages: string[]
      excludedPackages: string[]
      breakOnUpgrades: bool
      severities: string[]
      breakOnVulnerabilities: bool
      breakOnDeprecations: bool
      includeTransitives: bool }

type CommandContext =
    { options: OptionsContext 
      report: ReportContext
      github: GithubContext }

module CliCommands =

    let console = Spectre.Console.AnsiConsole.MarkupLine

    let trace traceLogging =
        if traceLogging then Console.grey >> console else ignore

    let returnError error =
        error |> Console.error |> console
        ReturnCodes.sysError

    let renderTables (values: seq<Spectre.Console.Table>) =
        values |> Seq.iter Spectre.Console.AnsiConsole.Write

    let renderBanner (nuget: Tk.Nuget.INugetClient) = nuget |> App.banner |> console

    let renderReportLine reportFile =
        $"{System.Environment.NewLine}Report file [link={reportFile}]{reportFile}[/] built."
        |> Console.italic
        |> console

    let returnCode isSuccess =
        match isSuccess with
        | true -> ReturnCodes.validationOk
        | _ -> ReturnCodes.validationFailed

    let githubContet (settings: PackageGithubCommandSettings) =
        { GithubContext.commit = settings.GithubCommit
          token = settings.GithubToken
          repo = settings.GithubRepo
          summaryTitle = settings.GithubSummaryTitle
          prId = settings.GithubPrId }

    let reportContext (settings: PackageGithubCommandSettings) =
        { ReportContext.reportDirectory = settings.OutputDirectory
          goodImageUri = settings.GoodImageUri 
          baddImageUri = settings.BadImageUri }

    let optionsContext (settings: PackageGithubCommandSettings) =
        { OptionsContext.projectPath = settings.ProjectPath
          suppressBanner = settings.NoBanner
          suppressRestore = settings.NoRestore
          includedPackages = settings.IncludedPackages |> Option.nullDefault [||] |> Array.filter String.isNotEmpty
          excludedPackages = settings.ExcludedPackages |> Option.nullDefault [||] |> Array.filter String.isNotEmpty
          breakOnUpgrades = false
          severities = [||]
          breakOnVulnerabilities = false
          breakOnDeprecations = false
          includeTransitives = false }