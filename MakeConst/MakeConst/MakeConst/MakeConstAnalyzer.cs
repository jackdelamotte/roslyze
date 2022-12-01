using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace MakeConst
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MakeConstAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MakeConst";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }
        
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            //Debugger.Launch();

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            //context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
            //context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.LocalDeclarationStatement);
            //context.RegisterSyntaxNodeAction(AnalyzeSetNode, SyntaxKind.SetAccessorDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeSwitch, SyntaxKind.SwitchStatement);
            //context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.IfStatement);
            //context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.VariableDeclaration);
        }

        //private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        //{
        //    var ifStatement = (IfStatementSyntax)context.Node;
        //    var diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation());
        //    context.ReportDiagnostic(diagnostic);
        //}

        // this will be for new()s

        //private static void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        //{
        //    // Find implicitly typed variable declarations.
        //    VariableDeclarationSyntax declaration = (VariableDeclarationSyntax)context.Node;
        //    if (declaration.Type.IsVar)
        //    {
        //        foreach (VariableDeclaratorSyntax variable in declaration.Variables)
        //        {
        //            // For all such locals, report a diagnostic.
        //            context.ReportDiagnostic(
        //                Diagnostic.Create(
        //                    Rule,
        //                    variable.GetLocation(),
        //                    variable.Identifier.ValueText));
        //        }
        //    }
        //}

        //private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        //{
        //    // This cast always succeeds because your analyzer registered for changes to local declarations, and only local declarations.
        //    var localDeclaration = (LocalDeclarationStatementSyntax)context.Node;
        //    //var variableDeclaration = (VariableDeclarationSyntax)context.Node;

        //    // make sure the declaration isn't already const:
        //    if (localDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword))
        //    {
        //        return;
        //    }

        //    // Perform data flow analysis on the local declaration.
        //    // TODO: read this article on the semantic model: https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/work-with-semantics
        //    DataFlowAnalysis dataFlowAnalysis = context.SemanticModel.AnalyzeDataFlow(localDeclaration);

        //    // Retrieve the local symbol for each variable in the local declaration
        //    // and ensure that it is not written outside of the data flow analysis region.
        //    VariableDeclaratorSyntax variable = localDeclaration.Declaration.Variables.Single();
        //    ISymbol variableSymbol = context.SemanticModel.GetDeclaredSymbol(variable, context.CancellationToken);
        //    if (dataFlowAnalysis.WrittenOutside.Contains(variableSymbol))
        //    {
        //        return;
        //    }

        //    context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), localDeclaration.Declaration.Variables.First().Identifier.ValueText));
        //}

        // switch statements
        private void AnalyzeSwitch(SyntaxNodeAnalysisContext context)
        {
            var switchStatement = (SwitchStatementSyntax)context.Node;

            // determine if each branch in the switch statement is assigning a value to a variable
            // if so, report diagnostic identifying this switch statement as one that could be converted to a switch expression

            // iterate over branches / sections
            bool conditionsHold = true;
            ExpressionSyntax left = null;
            foreach (SwitchSectionSyntax section in switchStatement.Sections) {

                bool matchesLeft = true;
                var assignment = section.Statements.First().ChildNodes().FirstOrDefault();
                if (assignment.GetType().Equals(typeof(AssignmentExpressionSyntax)))
                {
                    AssignmentExpressionSyntax a = (AssignmentExpressionSyntax)assignment;
                    if (left is null) { left = a.Left; }
                    else if (!left.WithoutTrivia().ToString().Equals(a.Left.WithoutTrivia().ToString())) { matchesLeft = false; }
                }

                if (!(section.Statements.Count == 2
                    && section.Statements.First().ChildNodes().FirstOrDefault().IsKind(SyntaxKind.SimpleAssignmentExpression)
                    && matchesLeft // either left is null (this is the first section) or the left side is same as the first left side
                    && section.Statements.Last().GetType().Equals(typeof(BreakStatementSyntax)))) {

                    conditionsHold = false;
                }
            }
            if (conditionsHold) context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), $"Can be converted..."));
            //else context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), $"didn't gettem"));

            int p = 0, q = 0;
            switch (p)
            {
                case 0:
                    p = q;
                    break;
                default:
                    break;
            }
        }
        private void AnalyzeSetNode(SyntaxNodeAnalysisContext context)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), "set"));
        }

        //private static void AnalyzeSymbol(SymbolAnalysisContext context)
        //{
        //    // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
        //    var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

        //    // Find just those named type symbols with names containing lowercase letters.
        //    if (namedTypeSymbol.Name.ToCharArray().Any(char.IsLower))
        //    {
        //        // For all such symbols, produce a diagnostic.
        //        var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

        //        context.ReportDiagnostic(diagnostic);
        //    }
        //}
    }
    }


