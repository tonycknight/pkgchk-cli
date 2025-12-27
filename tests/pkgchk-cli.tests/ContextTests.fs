namespace pkgchk.tests

open FsCheck.Xunit

module ContextTests =
    
    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], MaxTest = 1000)>]
    let ``applyConfig applies config over OptionsContext`` (context: pkgchk.OptionsContext, config: pkgchk.ScanConfiguration) =
        let r = pkgchk.Context.applyConfig context config

        let suppressBanner = r.suppressBanner = (match config.noBanner.HasValue with
                                                 | true -> config.noBanner.Value
                                                 | false -> context.suppressBanner)
        
        let suppressRestore = r.suppressRestore = (match config.noRestore.HasValue with
                                                    | true -> config.noRestore.Value
                                                    | false -> context.suppressRestore)

        let projectPath = (r.projectPath = context.projectPath)

        let includedPackages = r.includedPackages = (match config.includedPackages with
                                                     | null -> context.includedPackages
                                                     | x -> config.includedPackages)

        let excludedPackages = r.excludedPackages = (match config.excludedPackages with
                                                     | null -> context.excludedPackages
                                                     | x -> config.excludedPackages)

        let breakOnUpgrades = r.breakOnUpgrades = (match config.breakOnUpgrades.HasValue with
                                                    | true -> config.breakOnUpgrades.Value
                                                    | false -> context.breakOnUpgrades)

        let severities = r.severities = (match config.severities with
                                                     | null -> context.severities
                                                     | x -> config.severities)

        let breakOnVulnerabilities = r.breakOnVulnerabilities = (match config.breakOnVulnerabilities.HasValue with
                                                                    | true -> config.breakOnVulnerabilities.Value
                                                                    | false -> context.breakOnVulnerabilities)

        let breakOnDeprecations = r.breakOnDeprecations = (match config.breakOnDeprecations.HasValue with
                                                            | true -> config.breakOnDeprecations.Value
                                                            | false -> context.breakOnDeprecations)

        let includeTransitives = r.includeTransitives = (match config.checkTransitives.HasValue with
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