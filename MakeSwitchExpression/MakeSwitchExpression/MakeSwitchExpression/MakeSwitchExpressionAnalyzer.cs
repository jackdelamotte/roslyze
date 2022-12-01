using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace MakeSwitchExpression
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MakeSwitchExpressionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MakeSwitchExpression";

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

            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(AnalyzeSwitchNode, SyntaxKind.SwitchStatement);
        }

        private void AnalyzeSwitchNode(SyntaxNodeAnalysisContext context)
        {
            //context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), $"Reporting diagnostic at start of AnalyzeSwitchNode..."));
            var switchStatement = (SwitchStatementSyntax)context.Node;

            // iterate over branches / sections
            bool conditionsHold = true;
            ExpressionSyntax left = null;
            foreach (SwitchSectionSyntax section in switchStatement.Sections)
            {

                bool matchesLeft = true;
                var assignment = section.Statements.First().ChildNodes().FirstOrDefault();

                // either left is null (this is the first section) or the left side is same as the first left side
                if (assignment.GetType().Equals(typeof(AssignmentExpressionSyntax)))
                {
                    AssignmentExpressionSyntax a = (AssignmentExpressionSyntax)assignment;
                    if (left is null) { left = a.Left; }
                    else if (!left.WithoutTrivia().ToString().Equals(a.Left.WithoutTrivia().ToString())) { matchesLeft = false; }
                }

                // exactly two statements in the section
                // first statement is a simple assignment
                // second statement is a break
                // same variable being assigned throughout the switch
                // if any of the above conditions are false, conditionsHold is set to false
                // and thus, a diagnostic will not be reported.
                if (!(section.Statements.Count == 2
                    && section.Statements.First().ChildNodes().FirstOrDefault().IsKind(SyntaxKind.SimpleAssignmentExpression)
                    && section.Statements.Last().GetType().Equals(typeof(BreakStatementSyntax))
                    && matchesLeft))
                {
                    conditionsHold = false;
                }
            }
            if (conditionsHold) context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
        }
    }
}
