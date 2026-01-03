namespace pkgchk

open pkgchk.Combinators

type GithubContext =
    { [<Newtonsoft.Json.JsonIgnore>]
      token: string
      repo: string
      summaryTitle: string
      prId: string
      commit: string }

type ReportContext =
    { reportDirectory: string
      formats: ReportFormat[]
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

    static member empty =
        { OptionsContext.projectPath = ""
          configFile = ""
          suppressBanner = false
          suppressRestore = false
          includePackages = [||]
          excludePackages = [||]
          breakOnUpgrades = false
          severities = [||]
          scanVulnerabilities = false
          scanDeprecations = false
          scanTransitives = false }

type ServiceContext = { trace: (string -> unit) }

type ApplicationContext =
    { options: OptionsContext
      report: ReportContext
      github: GithubContext
      [<Newtonsoft.Json.JsonIgnore>]
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
          formats = settings.ReportFormats
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

    let applyContext (overlay: OptionsContext) (source: OptionsContext) =
        let apply overlay source =
            match overlay = source with
            | true -> source
            | false -> overlay

        let applySequence overlay source =
            match overlay = source with
            | false when Seq.isEmpty overlay -> source
            | false -> overlay
            | true -> source

        { OptionsContext.projectPath = overlay.projectPath
          configFile = overlay.configFile
          suppressBanner = apply overlay.suppressBanner source.suppressBanner
          suppressRestore = apply overlay.suppressRestore source.suppressRestore
          includePackages = applySequence overlay.includePackages source.includePackages
          excludePackages = applySequence overlay.excludePackages source.excludePackages
          breakOnUpgrades = apply overlay.breakOnUpgrades source.breakOnUpgrades
          severities = applySequence overlay.severities source.severities
          scanVulnerabilities = apply overlay.scanVulnerabilities source.scanVulnerabilities
          scanDeprecations = apply overlay.scanDeprecations source.scanDeprecations
          scanTransitives = apply overlay.scanTransitives source.scanTransitives }


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
        | x when x <> "" ->
            x
            |> Io.fullPath
            |> Io.normalise
            |> Config.load
            |> applyConfig OptionsContext.empty
            |> applyContext context

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

        let isIdMatch (hit: ScaHit) (map: string)=
            let eq x y = System.StringComparer.InvariantCultureIgnoreCase.Equals(x,y)
            if map.EndsWith("*") then
                let name = map.Substring(0, map.Length - 1)
                eq name hit.packageId
            else
                eq map hit.packageId

        let isHitMatch (map: string[]) (hit: ScaHit) =
            match map with
            | [||] -> true
            | xs -> map |> Seq.exists (isIdMatch hit)
            
        let included (hit: ScaHit) =
            match inclusionMap.Count with
            | 0 -> true
            | x -> inclusionMap.Contains hit.packageId

        let excluded (hit: ScaHit) =
            match exclusionMap.Count with
            | 0 -> true
            | x -> exclusionMap.Contains hit.packageId |> not

        hits |> Seq.filter (included &&>> excluded)

    let trace (context: ApplicationContext) =

        [ "Parameters:"; context |> Json.serialise |> String.escapeMarkup ]
        |> String.joinLines
        |> context.services.trace

        context
