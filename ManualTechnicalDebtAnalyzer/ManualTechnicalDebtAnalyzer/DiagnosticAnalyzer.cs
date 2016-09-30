using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Diagnostics;
using System.IO;

namespace ManualTechnicalDebtAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ManualTechnicalDebtAnalyzer : DiagnosticAnalyzer
    {
        public const string ManualDebt1DayDiagnosticId = "ManualDebt1Day";
        public const string ManualDebtMultipleDaysDiagnosticId = "ManualDebt{0}Days";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "TechnicalDebt";

        //private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(ManualDebt1DayDiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        private static Dictionary<string, DiagnosticDescriptor> _rules = new Dictionary<string,DiagnosticDescriptor>();

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(_rules.Values.ToArray());
            }
        }

        private static void CreateRules()
        {
            _rules.Add(ManualDebt1DayDiagnosticId, GetManualDebtInDaysDiagnosticDescriptor(ManualDebt1DayDiagnosticId, 1));

            for (int i = 2; i < 10; i++)
            {
                CreateRuleWithDayNumber(i);
            }

            for (int i = 10; i < 100; i += 10)
            {
                CreateRuleWithDayNumber(i);
            }
        }

        private static DiagnosticDescriptor GetManualDebtInDaysDiagnosticDescriptor(string id, int remediationCostInDays)
        {
            return new DiagnosticDescriptor(id,
                                            new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources), remediationCostInDays.ToString()), 
                                            new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources), remediationCostInDays.ToString()), 
                                            Category, 
                                            DiagnosticSeverity.Info, 
                                            isEnabledByDefault: true, 
                                            description: Description);
        }

        private static void CreateRuleWithDayNumber(int remediationCost)
        {
            var id = String.Format(ManualDebtMultipleDaysDiagnosticId, remediationCost);
            _rules.Add(id, GetManualDebtInDaysDiagnosticDescriptor(id, remediationCost));
        }

        static ManualTechnicalDebtAnalyzer()
        {
             CreateRules();
        }


        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.Attribute);
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
            var attributeSyntax = (AttributeSyntax)context.Node;

            var identifierNameSyntax = attributeSyntax.Name as IdentifierNameSyntax;

            if (identifierNameSyntax == null)
            {
                var qualifiedNameSyntax = attributeSyntax.Name as QualifiedNameSyntax;

                if (qualifiedNameSyntax != null)
                {
                    identifierNameSyntax = (IdentifierNameSyntax)qualifiedNameSyntax.DescendantNodes().FirstOrDefault((x) => x.IsKind(SyntaxKind.IdentifierName));
                }
            }            

            var attributeName = identifierNameSyntax.Identifier.Text;

            // Find just those named type symbols with names containing lowercase letters.
            if (attributeName.Equals("ManualTechnicalDebt", StringComparison.OrdinalIgnoreCase) ||
                attributeName.Equals("ManualTechnicalDebtAttribute", StringComparison.OrdinalIgnoreCase))
            {
                var sqaleRemediationDays = GetSqaleRemediationDays(attributeSyntax);
                                
                var rules = GetRulesInDays(sqaleRemediationDays);
                
                foreach (var rule in rules)
                {
                    context.ReportDiagnostic(Diagnostic.Create(rule, attributeSyntax.GetLocation(), attributeSyntax.Name));
                }                
            }
        }

        private static IEnumerable<DiagnosticDescriptor> GetRulesInDays(int sqaleRemediationDays)
        {
            if (sqaleRemediationDays < 100)
            {
                var remainingDays = sqaleRemediationDays;

                if (sqaleRemediationDays > 9)
                {
                    var tensCount = sqaleRemediationDays / 10;
                    var flooredToTens = tensCount * 10;
                    yield return _rules[String.Format(ManualDebtMultipleDaysDiagnosticId, flooredToTens)];

                    remainingDays = sqaleRemediationDays - flooredToTens;
                }

                if (remainingDays > 0)
                    yield return GetBelowTenDaysManualRule(remainingDays);
            }
            else
            {
                // Return max value
                yield return _rules[String.Format(ManualDebtMultipleDaysDiagnosticId, 90)];
                yield return _rules[String.Format(ManualDebtMultipleDaysDiagnosticId, 9)];
            }
        }

        private static DiagnosticDescriptor GetBelowTenDaysManualRule(int sqaleRemediationDays)
        {
            if (sqaleRemediationDays == 1)
                return _rules[ManualDebt1DayDiagnosticId];
            else
                return _rules[String.Format(ManualDebtMultipleDaysDiagnosticId, sqaleRemediationDays)];
        }

        private static int GetSqaleRemediationDays(AttributeSyntax attributeSyntax)
        {
            AttributeArgumentSyntax sqaleRemediationDaysArgument = GetAttributeArgumentSyntax(attributeSyntax, "SqaleRemediationDaysEffort");

            return GetAttributeArgumentNumericValue(sqaleRemediationDaysArgument);
        }

        private static AttributeArgumentSyntax GetAttributeArgumentSyntax(AttributeSyntax attributeSyntax, string attributeArgumentName)
        {
            var attributeArgumentNodes = attributeSyntax.DescendantNodes().Where((x) => x.IsKind(SyntaxKind.AttributeArgument)).Cast<AttributeArgumentSyntax>();

            var sqaleRemediationArgument =
                attributeArgumentNodes.FirstOrDefault(
                    (attributeArgumentNode) => attributeArgumentNode.DescendantTokens().Where((y) => y.IsKind(SyntaxKind.IdentifierToken) &&
                                                                                                     y.Text == attributeArgumentName).Any());
            return sqaleRemediationArgument;
        }

        private static int GetAttributeArgumentNumericValue(AttributeArgumentSyntax sqaleRemediationArgument)
        {
            if (sqaleRemediationArgument != null)
            {
                SyntaxToken numericValueToken = sqaleRemediationArgument.DescendantTokens().FirstOrDefault((token) => token.IsKind(SyntaxKind.NumericLiteralToken));
                return UInt16.Parse(numericValueToken.Text);
            }

            return 0;
        }
    }
}
