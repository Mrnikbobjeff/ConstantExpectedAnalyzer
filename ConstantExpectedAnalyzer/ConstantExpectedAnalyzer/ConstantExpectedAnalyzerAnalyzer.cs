using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ConstantExpectedAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConstantExpectedAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ConstantExpectedAnalyzer";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Performance";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.InvocationExpression);
        }


        static bool IsConstant(ExpressionSyntax syntax, SyntaxNodeAnalysisContext context)
        {
            return context.SemanticModel.GetSymbolInfo(syntax).Symbol is IFieldSymbol info && info.IsConst;
        }
        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (!(context.SemanticModel.GetSymbolInfo(invocation.Expression).Symbol is IMethodSymbol methodSymbol))
                return;

            for (int i = 0; i < methodSymbol.Parameters.Length; i++)
            {
                IParameterSymbol argument = (IParameterSymbol)methodSymbol.Parameters[i];
                if (!argument.GetAttributes().Any(x => x.AttributeClass.Name.Equals("ConstantExpectedAttribute")))
                    continue;
                //Attribute present x => x is LiteralExpressionSyntax ||
                var parameterValue = invocation.ArgumentList.Arguments[i].Expression;
                if (parameterValue is LiteralExpressionSyntax || IsConstant(parameterValue, context))
                    return; // Constant is passed

                var diagnostic = Diagnostic.Create(Rule, parameterValue.GetLocation());

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
