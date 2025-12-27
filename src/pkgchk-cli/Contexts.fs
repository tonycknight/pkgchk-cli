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
      goodImageUri: string
      badImageUri: string }

type OptionsContext =
    { projectPath: string
      configFile: string
      suppressBanner: bool
      suppressRestore: bool
      includedPackages: string[]
      excludedPackages: string[]
      breakOnUpgrades: bool
      severities: string[]
      breakOnVulnerabilities: bool
      breakOnDeprecations: bool
      includeTransitives: bool }

type ServiceContext =
    { trace: (string -> unit) }

type ApplicationContext = // TODO: need a trace/collection of functions/properly named
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
          goodImageUri = settings.GoodImageUri
          badImageUri = settings.BadImageUri }

    let optionsContext (settings: PackageGithubCommandSettings) =
        { OptionsContext.projectPath = settings.ProjectPath
          configFile = settings.ConfigFile
          suppressBanner = settings.NoBanner
          suppressRestore = settings.NoRestore
          includedPackages =
            settings.IncludedPackages
            |> Option.nullDefault [||]
            |> Array.filter String.isNotEmpty
          excludedPackages =
            settings.ExcludedPackages
            |> Option.nullDefault [||]
            |> Array.filter String.isNotEmpty
          breakOnUpgrades = false
          severities = [||]
          breakOnVulnerabilities = false
          breakOnDeprecations = false
          includeTransitives = false }

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
                breakOnVulnerabilities = settings.IncludeVulnerables
                breakOnDeprecations = settings.IncludeDeprecations
                includeTransitives = settings.IncludeTransitives }
        
        options |> applicationContext settings
        
    let listContext (settings: PackageListCommandSettings) =
        let options =
            { optionsContext settings with
                includeTransitives = settings.IncludeTransitives }

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

        if config.includedPackages |> Option.isNull |> not then
            result <-
                { result with
                    includedPackages = config.includedPackages }

        if config.excludedPackages |> Option.isNull |> not then
            result <-
                { result with
                    excludedPackages = config.excludedPackages }

        if config.breakOnUpgrades.HasValue then
            result <-
                { result with
                    breakOnUpgrades = config.breakOnUpgrades.Value }

        if config.severities |> Option.isNull |> not then
            result <-
                { result with
                    severities = config.severities }

        if config.breakOnVulnerabilities.HasValue then
            result <-
                { result with
                    breakOnVulnerabilities = config.breakOnVulnerabilities.Value }

        if config.breakOnDeprecations.HasValue then
            result <-
                { result with
                    breakOnDeprecations = config.breakOnDeprecations.Value }

        if config.checkTransitives.HasValue then
            result <-
                { result with
                    includeTransitives = config.checkTransitives.Value }

        result

    let loadApplyConfig (context: OptionsContext) =
        match context.configFile with
        | x when x <> "" -> x |> Io.fullPath |> Io.normalise |> Config.load |> applyConfig context
        | _ -> context

    let filterPackages (context: OptionsContext) (hits: seq<pkgchk.ScaHit>) =
        let inclusionMap =
            context.includedPackages
            |> HashSet.ofSeq System.StringComparer.InvariantCultureIgnoreCase

        let exclusionMap =
            context.excludedPackages
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
