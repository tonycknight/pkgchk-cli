namespace pkgchk.tests

open FsCheck.Xunit

type ReportContext = pkgchk.ReportContext
type ServiceContext = pkgchk.ServiceContext
type OptionsContext = pkgchk.OptionsContext
type GithubContext = pkgchk.GithubContext
type ApplicationContext = pkgchk.ApplicationContext

type ReportGenerationContext = pkgchk.ReportGenerationContext
type ApplicationScanResults = pkgchk.ApplicationScanResults

module ReportGenerationTests =

    let rptContext =
        { ReportContext.badImageUri = ""
          ReportContext.formats = [||]
          ReportContext.goodImageUri = ""
          ReportContext.reportDirectory = "" }

    let svcContext = { ServiceContext.trace = ignore }

    let optContext =
        { OptionsContext.includePackages = [||]
          OptionsContext.excludePackages = [||]
          OptionsContext.suppressBanner = false
          OptionsContext.suppressRestore = false
          OptionsContext.breakOnUpgrades = false
          OptionsContext.projectPath = ""
          OptionsContext.configFile = ""
          OptionsContext.scanVulnerabilities = false
          OptionsContext.scanDeprecations = false
          OptionsContext.scanTransitives = false
          OptionsContext.severities = [||]
          OptionsContext.fetchMetadata = true }

    let ghContext =
        { GithubContext.prId = ""
          GithubContext.repo = ""
          GithubContext.summaryTitle = ""
          GithubContext.commit = ""
          GithubContext.token = ""
          GithubContext.noCheck = false }

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``jsonReport builds Json representation of hits`` (hits: pkgchk.ScaHit[]) =
        let appContext =
            { ApplicationContext.github = ghContext
              ApplicationContext.options = optContext
              ApplicationContext.report = rptContext
              ApplicationContext.services = svcContext }

        let results =
            { ApplicationScanResults.hits = List.ofSeq hits
              ApplicationScanResults.hitCounts = []
              ApplicationScanResults.isGoodScan = false }

        let result =
            pkgchk.ReportGeneration.jsonReport (appContext, results, "")
            |> pkgchk.String.joinLines

        let check = pkgchk.Json.deserialise<pkgchk.ScaHit[]> result

        check = hits

    [<Property(Arbitrary = [| typeof<AlphaNumericString> |], Verbose = true)>]
    let ``reports generates reports at the given directory`` (hits: pkgchk.ScaHit[], dir: System.Guid) =
        let directory = dir.ToString()

        let rptContext =
            { rptContext with
                reportDirectory = $"./testdata/{directory}"
                formats = [| pkgchk.ReportFormat.Json; pkgchk.ReportFormat.Markdown |] }

        let appContext =
            { ApplicationContext.github = ghContext
              ApplicationContext.options = optContext
              ApplicationContext.report = rptContext
              ApplicationContext.services = svcContext }

        let results =
            { ApplicationScanResults.hits = List.ofSeq hits
              ApplicationScanResults.hitCounts = []
              ApplicationScanResults.isGoodScan = false }

        let rptContext =
            { ReportGenerationContext.reportName = "test"
              ReportGenerationContext.results = results
              ReportGenerationContext.imageUri = ""
              ReportGenerationContext.app = appContext
              ReportGenerationContext.genJson = pkgchk.ReportGeneration.jsonReport
              ReportGenerationContext.genMarkdown = (fun (c, r, _) -> [ r.hits.ToString() ]) }

        let reportPaths = pkgchk.ReportGeneration.reports rptContext

        reportPaths |> Seq.map System.IO.File.Exists |> Seq.length = List.length reportPaths
