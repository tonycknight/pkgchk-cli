namespace pkgchk.tests

open FsCheck.Xunit

module ContextTests =
    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``githubContext maps Github properties`` (settings: pkgchk.PackageGithubCommandSettings) =
        let r = pkgchk.Context.githubContext settings

        r.commit = settings.GithubCommit
        && r.prId = settings.GithubPrId
        && r.summaryTitle = settings.GithubSummaryTitle
        && r.repo = settings.GithubRepo
        && r.token = settings.GithubToken

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``reportContext maps report properties`` (settings: pkgchk.PackageGithubCommandSettings) =
        let r = pkgchk.Context.reportContext settings

        r.badImageUri = settings.BadImageUri
        && r.formats = settings.ReportFormats
        && r.goodImageUri = settings.GoodImageUri
        && r.reportDirectory = settings.OutputDirectory

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``optionsContext maps options properties`` (settings: pkgchk.PackageGithubCommandSettings) =
        let r = pkgchk.Context.optionsContext settings

        r.projectPath = settings.ProjectPath
        && r.configFile = settings.ConfigFile
        && r.suppressBanner = settings.NoBanner
        && r.suppressRestore = settings.NoRestore
        && r.includePackages = settings.IncludedPackages
        && r.excludePackages = settings.ExcludedPackages
        && r.breakOnUpgrades = false
        && r.severities = [||]
        && r.scanVulnerabilities = false
        && r.scanDeprecations = false
        && r.scanTransitives = false

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``scanContext maps properties`` (settings: pkgchk.PackageScanCommandSettings) =
        let r = pkgchk.Context.scanContext settings

        r.options.severities = (settings.SeverityLevels |> Array.filter pkgchk.String.isNotEmpty)
        && r.options.scanVulnerabilities = settings.IncludeVulnerables
        && r.options.scanDeprecations = settings.IncludeDeprecations
        && r.options.scanTransitives = settings.IncludeTransitives
        && r.github = (pkgchk.Context.githubContext settings)
        && r.report = (pkgchk.Context.reportContext settings)

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``listContext maps properties`` (settings: pkgchk.PackageListCommandSettings) =
        let r = pkgchk.Context.listContext settings

        r.options.scanTransitives = settings.IncludeTransitives
        && r.github = (pkgchk.Context.githubContext settings)
        && r.report = (pkgchk.Context.reportContext settings)

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``upgradesContext maps properties`` (settings: pkgchk.PackageUpgradeCommandSettings) =
        let r = pkgchk.Context.upgradesContext settings

        r.options.breakOnUpgrades = settings.BreakOnUpgrades
        && r.github = (pkgchk.Context.githubContext settings)
        && r.report = (pkgchk.Context.reportContext settings)

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``applyConfig applies config over OptionsContext``
        (context: pkgchk.OptionsContext, config: pkgchk.ScanConfiguration)
        =
        let r = pkgchk.Context.applyConfig context config

        let suppressBanner =
            r.suppressBanner = (match config.noBanner.HasValue with
                                | true -> config.noBanner.Value
                                | false -> context.suppressBanner)

        let suppressRestore =
            r.suppressRestore = (match config.noRestore.HasValue with
                                 | true -> config.noRestore.Value
                                 | false -> context.suppressRestore)

        let projectPath = (r.projectPath = context.projectPath)

        let includedPackages =
            r.includePackages = (match config.includePackages with
                                 | null -> context.includePackages
                                 | x -> config.includePackages)

        let excludedPackages =
            r.excludePackages = (match config.excludePackages with
                                 | null -> context.excludePackages
                                 | x -> config.excludePackages)

        let breakOnUpgrades =
            r.breakOnUpgrades = (match config.breakOnUpgrades.HasValue with
                                 | true -> config.breakOnUpgrades.Value
                                 | false -> context.breakOnUpgrades)

        let severities =
            r.severities = (match config.severities with
                            | null -> context.severities
                            | x -> config.severities)

        let scanVulnerabilities =
            r.scanVulnerabilities = (match config.scanVulnerabilities.HasValue with
                                     | true -> config.scanVulnerabilities.Value
                                     | false -> context.scanVulnerabilities)

        let scanDeprecations =
            r.scanDeprecations = (match config.scanDeprecations.HasValue with
                                  | true -> config.scanDeprecations.Value
                                  | false -> context.scanDeprecations)

        let includeTransitives =
            r.scanTransitives = (match config.scanTransitives.HasValue with
                                 | true -> config.scanTransitives.Value
                                 | false -> context.scanTransitives)

        suppressBanner
        && suppressRestore
        && projectPath
        && includedPackages
        && excludedPackages
        && breakOnUpgrades
        && severities
        && scanVulnerabilities
        && scanDeprecations
        && includeTransitives

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``filterPackages empty sets includes package`` (context: pkgchk.OptionsContext, hit: pkgchk.ScaHit) =
        let hits = [ hit ]

        let context =
            { context with
                excludePackages = [||]
                includePackages = [||] }

        let r = pkgchk.Context.filterPackages context hits |> List.ofSeq

        r = hits

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``filterPackages includes package`` (context: pkgchk.OptionsContext, hit: pkgchk.ScaHit) =
        let hits = [ hit ]

        let context =
            { context with
                excludePackages = [||]
                includePackages = [| hit.packageId |] }

        let r = pkgchk.Context.filterPackages context hits |> List.ofSeq

        r = hits

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``filterPackages with wildcard includes package`` (context: pkgchk.OptionsContext, hit: pkgchk.ScaHit) =
        let hits = [ hit ]

        let context =
            { context with
                excludePackages = [||]
                includePackages = [| $"{hit.packageId}*" |] }

        let r = pkgchk.Context.filterPackages context hits |> List.ofSeq

        r = hits

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``filterPackages does not include package`` (context: pkgchk.OptionsContext, hit: pkgchk.ScaHit) =
        let hits = [ hit ]

        let context =
            { context with
                excludePackages = [||]
                includePackages = [| (hit.packageId + hit.packageId) |] }

        let r = pkgchk.Context.filterPackages context hits |> List.ofSeq

        List.isEmpty r

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``filterPackages excludes package`` (context: pkgchk.OptionsContext, hit: pkgchk.ScaHit) =
        let hits = [ hit ]

        let context =
            { context with
                includePackages = [||]
                excludePackages = [| hit.packageId |] }

        let r = pkgchk.Context.filterPackages context hits |> List.ofSeq

        List.isEmpty r

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``filterPackages with wildcard excludes package`` (context: pkgchk.OptionsContext, hit: pkgchk.ScaHit) =
        let packageId = hit.packageId

        let hit =
            { hit with
                packageId = hit.packageId + "Extra" } // extend the package ID to match by wildcard

        let hits = [ hit ]

        let context =
            { context with
                includePackages = [||]
                excludePackages = [| $"{packageId}*" |] }

        let r = pkgchk.Context.filterPackages context hits |> List.ofSeq

        List.isEmpty r


    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``filterPackages does not exclude package`` (context: pkgchk.OptionsContext, hit: pkgchk.ScaHit) =
        let hits = [ hit ]

        let context =
            { context with
                includePackages = [||]
                excludePackages = [| (hit.packageId + hit.packageId) |] }

        let r = pkgchk.Context.filterPackages context hits |> List.ofSeq

        r = hits

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``filterPackages excludes includes package`` (context: pkgchk.OptionsContext, hit: pkgchk.ScaHit) =
        let hits = [ hit ]

        let context =
            { context with
                includePackages = [| hit.packageId |]
                excludePackages = [| hit.packageId |] }

        let r = pkgchk.Context.filterPackages context hits |> List.ofSeq

        r |> List.isEmpty

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``applyContext applies overlay to source`` (source: pkgchk.OptionsContext, overlay: pkgchk.OptionsContext) =
        let result = pkgchk.Context.applyContext overlay source

        result.suppressBanner = overlay.suppressBanner
        && result.suppressRestore = overlay.suppressRestore
        && result.breakOnUpgrades = overlay.breakOnUpgrades
        && result.scanDeprecations = overlay.scanDeprecations
        && result.scanVulnerabilities = overlay.scanVulnerabilities
        && result.scanTransitives = overlay.scanTransitives
        && result.projectPath = overlay.projectPath
        && result.configFile = overlay.configFile

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``applyContext applies overlay to source when overlay's sequence is empty``
        (source: pkgchk.OptionsContext, overlay: pkgchk.OptionsContext)
        =

        let comp xs ys zs =
            match xs with
            | [||] -> zs = ys
            | _ -> zs = xs

        let result = pkgchk.Context.applyContext overlay source

        result.includePackages |> comp overlay.includePackages source.includePackages
        && result.excludePackages |> comp overlay.excludePackages source.excludePackages
        && result.severities |> comp overlay.severities source.severities

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``applyContext applies overlay to empty`` (overlay: pkgchk.OptionsContext) =
        let empty = pkgchk.OptionsContext.empty
        let result = pkgchk.Context.applyContext overlay empty

        result.suppressBanner = overlay.suppressBanner
        && result.suppressRestore = overlay.suppressRestore
        && result.breakOnUpgrades = overlay.breakOnUpgrades
        && result.excludePackages = overlay.excludePackages
        && result.includePackages = overlay.includePackages
        && result.scanDeprecations = overlay.scanDeprecations
        && result.scanVulnerabilities = overlay.scanVulnerabilities
        && result.scanTransitives = overlay.scanTransitives
        && result.projectPath = overlay.projectPath
        && result.configFile = overlay.configFile

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``applyContext applies overlay to self`` (overlay: pkgchk.OptionsContext) =
        let result = pkgchk.Context.applyContext overlay overlay

        result.suppressBanner = overlay.suppressBanner
        && result.suppressRestore = overlay.suppressRestore
        && result.breakOnUpgrades = overlay.breakOnUpgrades
        && result.excludePackages = overlay.excludePackages
        && result.includePackages = overlay.includePackages
        && result.scanDeprecations = overlay.scanDeprecations
        && result.scanVulnerabilities = overlay.scanVulnerabilities
        && result.scanTransitives = overlay.scanTransitives
        && result.projectPath = overlay.projectPath
        && result.configFile = overlay.configFile
