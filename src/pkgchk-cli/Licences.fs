namespace pkgchk

open System
open System.Linq
open Spdx.Expressions

type private LicenseVisitor() =

    inherit SpdxExpressionVisitor<Object, seq<string>>()

    override this.VisitAnd(context: Object, expression: SpdxAndExpression) : seq<string> =
        expression.Left.Accept(context, this).Concat(expression.Right.Accept(context, this))

    override this.VisitException(context: Object, expression: SpdxLicenseExceptionExpression) : seq<string> = seq { }

    override this.VisitLicense(conteext: Object, expression: SpdxLicenseExpression) : seq<string> =
        seq { expression.Id }

    override this.VisitOr(context: Object, expression: SpdxOrExpression) : seq<string> =
        expression.Left.Accept(context, this).Concat(expression.Right.Accept(context, this))

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
