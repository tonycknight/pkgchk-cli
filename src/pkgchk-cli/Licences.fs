namespace pkgchk

open System
open Spdx.Expressions

type private LicenseVisitor() =

    inherit SpdxExpressionVisitor<Object, seq<string>>()

    override this.VisitAnd(context: Object, expression: SpdxAndExpression) : seq<string> =
        let l = expression.Left.Accept(context, this)
        let r = expression.Right.Accept(context, this)

        r |> Seq.append l

    override this.VisitException(context: Object, expression: SpdxLicenseExceptionExpression) : seq<string> = seq { }

    override this.VisitLicense(context: Object, expression: SpdxLicenseExpression) : seq<string> = seq { expression.Id }

    override this.VisitOr(context: Object, expression: SpdxOrExpression) : seq<string> =
        let l = expression.Left.Accept(context, this)
        let r = expression.Right.Accept(context, this)

        r |> Seq.append l

    override this.VisitReference(context: Object, expression: SpdxLicenseReferenceExpression) : seq<string> = seq { }

    override this.VisitScope(context: Object, expression: SpdxScopeExpression) : seq<string> =
        expression.Expression.Accept(context, this)

    override this.VisitWith(context: Object, expression: SpdxWithExpression) : seq<string> =
        expression.Expression.Accept(context, this)

module Licences =

    let parse expression =
        try
            let spdxExpr = SpdxAndExpression.Parse(expression, SpdxLicenseOptions.Relaxed)

            let v = new LicenseVisitor()

            v.Visit(null, spdxExpr)
        with :? SpdxParseException ->
            [ expression ]

    let licence (hit: ScaHit) =
        // TODO: extract from expression
        let get (meta: NugetPackageMetadata) =
            match (meta.license, meta.licenseUrl) with
            | ("", Some url)
            | (null, Some url) -> url
            | (x, _) -> x
            |> Option.ofNull
            |> Option.defaultValue ""

        hit.metadata |> Option.map get
