namespace pkgchk

open pkgchk.Combinators

type GithubContext =
    { token: string
      repo: string
      summaryTitle: string
      prId: string
      commit: string }

type ReportContext =
    { reportDirectory: string
      reportFormats: ReportFormat[]
      goodImageUri: string
      badImageUri: string }

type OptionsContext =
    { projectPath: string
      configFile: string
      suppressBanner: bool
      suppressRestore: bool
      includePackages: string[]
      excludePackages: string[]
      breakOnUpgrades: bool
      severities: string[]
      scanVulnerabilities: bool
      scanDeprecations: bool
      scanTransitives: bool }

type ServiceContext = { trace: (string -> unit) }

type ApplicationContext =
    { options: OptionsContext
      report: ReportContext
      github: GithubContext
      services: ServiceContext }

module Context =
    let githubContext (settings: PackageGithubCommandSettings) =
        { GithubContext.commit = settings.GithubCommit
          token = settings.GithubToken
          repo = settings.GithubRepo
          summaryTitle = settings.GithubSummaryTitle
          prId = settings.GithubPrId }

    let reportContext (settings: PackageGithubCommandSettings) =
        { ReportContext.reportDirectory = settings.OutputDirectory
          reportFormats = settings.ReportFormats
          goodImageUri = settings.GoodImageUri
          badImageUri = settings.BadImageUri }

    let optionsContext (settings: PackageGithubCommandSettings) =
        { OptionsContext.projectPath = settings.ProjectPath
          configFile = settings.ConfigFile
          suppressBanner = settings.NoBanner
          suppressRestore = settings.NoRestore
          includePackages =
            settings.IncludedPackages
            |> Option.nullDefault [||]
            |> Array.filter String.isNotEmpty
          excludePackages =
            settings.ExcludedPackages
            |> Option.nullDefault [||]
            |> Array.filter String.isNotEmpty
          breakOnUpgrades = false
          severities = [||]
          scanVulnerabilities = false
          scanDeprecations = false
          scanTransitives = false }

    let serviceContext (settings: PackageCommandSettings) =
        { ServiceContext.trace = CliCommands.trace settings.TraceLogging }

    let applicationContext (settings: PackageGithubCommandSettings) (options: OptionsContext) =
        { ApplicationContext.options = options
          github = githubContext settings
          report = reportContext settings
          services = serviceContext settings }

    let scanContext (settings: PackageScanCommandSettings) =
        let options =
            { optionsContext settings with
                severities = settings.SeverityLevels |> Array.filter String.isNotEmpty
                scanVulnerabilities = settings.IncludeVulnerables
                scanDeprecations = settings.IncludeDeprecations
                scanTransitives = settings.IncludeTransitives }

        options |> applicationContext settings

    let listContext (settings: PackageListCommandSettings) =
        let options =
            { optionsContext settings with
                scanTransitives = settings.IncludeTransitives }

        options |> applicationContext settings

    let upgradesContext (settings: PackageUpgradeCommandSettings) =
        let options =
            { optionsContext settings with
                breakOnUpgrades = settings.BreakOnUpgrades }

        options |> applicationContext settings

    let applyConfig (context: OptionsContext) (config: ScanConfiguration) =
        let mutable result = context

        if config.noBanner.HasValue then
            result <-
                { context with
                    suppressBanner = config.noBanner.Value }

        if config.noRestore.HasValue then
            result <-
                { result with
                    suppressRestore = config.noRestore.Value }

        if config.includePackages |> Option.isNull |> not then
            result <-
                { result with
                    includePackages = config.includePackages }

        if config.excludePackages |> Option.isNull |> not then
            result <-
                { result with
                    excludePackages = config.excludePackages }

        if config.breakOnUpgrades.HasValue then
            result <-
                { result with
                    breakOnUpgrades = config.breakOnUpgrades.Value }

        if config.severities |> Option.isNull |> not then
            result <-
                { result with
                    severities = config.severities }

        if config.scanVulnerabilities.HasValue then
            result <-
                { result with
                    scanVulnerabilities = config.scanVulnerabilities.Value }

        if config.scanDeprecations.HasValue then
            result <-
                { result with
                    scanDeprecations = config.scanDeprecations.Value }

        if config.scanTransitives.HasValue then
            result <-
                { result with
                    scanTransitives = config.scanTransitives.Value }

        result

    let loadApplyConfig (context: OptionsContext) =
        match context.configFile with
        | x when x <> "" -> x |> Io.fullPath |> Io.normalise |> Config.load |> applyConfig context
        | _ -> context

    let hasGithubParameters (context: ApplicationContext) =
        String.isNotEmpty context.github.token
        && String.isNotEmpty context.github.repo
        && (String.isNotEmpty context.github.prId || String.isNotEmpty context.github.commit)

    let reportImage ok (context: ApplicationContext) =
        match ok with
        | true -> context.report.goodImageUri
        | false -> context.report.badImageUri

    let filterPackages (context: OptionsContext) (hits: seq<pkgchk.ScaHit>) =
        let inclusionMap =
            context.includePackages
            |> HashSet.ofSeq System.StringComparer.InvariantCultureIgnoreCase

        let exclusionMap =
            context.excludePackages
            |> HashSet.ofSeq System.StringComparer.InvariantCultureIgnoreCase

        let included (hit: ScaHit) =
            match inclusionMap.Count with
            | 0 -> true
            | x -> inclusionMap.Contains hit.packageId

        let excluded (hit: ScaHit) =
            match exclusionMap.Count with
            | 0 -> true
            | x -> exclusionMap.Contains hit.packageId |> not

        hits |> Seq.filter (included &&>> excluded)
