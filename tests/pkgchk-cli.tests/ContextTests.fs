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
        && r.goodImageUri = settings.GoodImageUri
        && r.reportDirectory = settings.OutputDirectory

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``optionsContext maps options properties`` (settings: pkgchk.PackageGithubCommandSettings) =
        let r = pkgchk.Context.optionsContext settings

        r.projectPath = settings.ProjectPath
        && r.suppressBanner = settings.NoBanner
        && r.suppressRestore = settings.NoRestore
        && r.includedPackages = settings.IncludedPackages
        && r.excludedPackages = settings.ExcludedPackages
        && r.breakOnUpgrades = false
        && r.severities = [||]
        && r.breakOnVulnerabilities = false
        && r.breakOnDeprecations = false
        && r.includeTransitives = false

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``scanContext maps properties`` (settings: pkgchk.PackageScanCommandSettings) =
        let r = pkgchk.Context.scanContext settings

        r.options.severities = (settings.SeverityLevels |> Array.filter pkgchk.String.isNotEmpty)
        && r.options.breakOnVulnerabilities = settings.IncludeVulnerables
        && r.options.breakOnDeprecations = settings.IncludeDeprecations
        && r.options.includeTransitives = settings.IncludeTransitives
        && r.github = (pkgchk.Context.githubContext settings)
        && r.report = (pkgchk.Context.reportContext settings)

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``listContext maps properties`` (settings: pkgchk.PackageListCommandSettings) =
        let r = pkgchk.Context.listContext settings

        r.options.includeTransitives = settings.IncludeTransitives
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
            r.includedPackages = (match config.includedPackages with
                                  | null -> context.includedPackages
                                  | x -> config.includedPackages)

        let excludedPackages =
            r.excludedPackages = (match config.excludedPackages with
                                  | null -> context.excludedPackages
                                  | x -> config.excludedPackages)

        let breakOnUpgrades =
            r.breakOnUpgrades = (match config.breakOnUpgrades.HasValue with
                                 | true -> config.breakOnUpgrades.Value
                                 | false -> context.breakOnUpgrades)

        let severities =
            r.severities = (match config.severities with
                            | null -> context.severities
                            | x -> config.severities)

        let breakOnVulnerabilities =
            r.breakOnVulnerabilities = (match config.breakOnVulnerabilities.HasValue with
                                        | true -> config.breakOnVulnerabilities.Value
                                        | false -> context.breakOnVulnerabilities)

        let breakOnDeprecations =
            r.breakOnDeprecations = (match config.breakOnDeprecations.HasValue with
                                     | true -> config.breakOnDeprecations.Value
                                     | false -> context.breakOnDeprecations)

        let includeTransitives =
            r.includeTransitives = (match config.checkTransitives.HasValue with
                                    | true -> config.checkTransitives.Value
                                    | false -> context.includeTransitives)

        suppressBanner
        && suppressRestore
        && projectPath
        && includedPackages
        && excludedPackages
        && breakOnUpgrades
        && severities
        && breakOnVulnerabilities
        && breakOnDeprecations
        && includeTransitives

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``filterPackages empty sets includes package`` (context: pkgchk.OptionsContext, hit: pkgchk.ScaHit) =
        let hits = [ hit ]
        let context = { context with excludedPackages = [||] ; includedPackages = [| |] }
        
        let r = pkgchk.Context.filterPackages context hits |> List.ofSeq

        r = hits

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``filterPackages includes package`` (context: pkgchk.OptionsContext, hit: pkgchk.ScaHit) =
        let hits = [ hit ]
        let context = { context with excludedPackages = [||] ; includedPackages = [| hit.packageId |] }
        
        let r = pkgchk.Context.filterPackages context hits |> List.ofSeq

        r = hits

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``filterPackages does not include package`` (context: pkgchk.OptionsContext, hit: pkgchk.ScaHit) =
        let hits = [ hit ]
        let context = { context with excludedPackages = [||] ; includedPackages = [| (hit.packageId + hit.packageId) |] }
        
        let r = pkgchk.Context.filterPackages context hits |> List.ofSeq

        List.isEmpty r

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``filterPackages excludes package`` (context: pkgchk.OptionsContext, hit: pkgchk.ScaHit) =
        let hits = [ hit ]
        let context = { context with includedPackages = [||] ; excludedPackages = [| hit.packageId |] }
        
        let r = pkgchk.Context.filterPackages context hits |> List.ofSeq

        List.isEmpty r

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``filterPackages does not exclude package`` (context: pkgchk.OptionsContext, hit: pkgchk.ScaHit) =
        let hits = [ hit ]
        let context = { context with includedPackages = [||] ; excludedPackages = [| (hit.packageId + hit.packageId) |] }
        
        let r = pkgchk.Context.filterPackages context hits |> List.ofSeq

        r = hits

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``filterPackages excludes includes package`` (context: pkgchk.OptionsContext, hit: pkgchk.ScaHit) =
        let hits = [ hit ]
        let context = { context with includedPackages = [| hit.packageId |] ; excludedPackages = [| hit.packageId |] }
        
        let r = pkgchk.Context.filterPackages context hits |> List.ofSeq

        r |> List.isEmpty