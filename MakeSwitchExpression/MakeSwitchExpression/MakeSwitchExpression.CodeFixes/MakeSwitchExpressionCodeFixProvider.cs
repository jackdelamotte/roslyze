using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MakeSwitchExpression
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSwitchExpressionCodeFixProvider)), Shared]
    public class MakeSwitchExpressionCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(MakeSwitchExpressionAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<SwitchStatementSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedDocument: c => MakeSwitchAsync(context.Document, declaration, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private static async Task<Document> MakeSwitchAsync(Document document, SwitchStatementSyntax switchStatement, CancellationToken cancellationToken)
        {
            SeparatedSyntaxList<SwitchExpressionArmSyntax> arms = new SeparatedSyntaxList<SwitchExpressionArmSyntax>();

            ExpressionSyntax left = null;

            foreach (SwitchSectionSyntax section in switchStatement.Sections)
            {
                PatternSyntax pattern = null; // will get set in either the if or the else

                if (section.ChildNodes().First().IsKind(SyntaxKind.CaseSwitchLabel))
                {
                    ExpressionSyntax patternExpression = ((CaseSwitchLabelSyntax)section.Labels.First()).Value;
                    // TODO: handle more kinds of patterns here
                    switch (patternExpression.Kind())
                    {
                        case SyntaxKind.NumericLiteralExpression:
                        case SyntaxKind.StringLiteralExpression:
                            pattern = SyntaxFactory.ConstantPattern(patternExpression);
                            break;
                    }
                }
                else
                {
                    pattern = SyntaxFactory.DiscardPattern();
                }
                
                var assignment = section.Statements.First().ChildNodes().FirstOrDefault();
                if (left is null) left = ((AssignmentExpressionSyntax)assignment).Left; // save this for later
                ExpressionSyntax expression = ((AssignmentExpressionSyntax)assignment).Right;

                arms = arms.Add(SyntaxFactory.SwitchExpressionArm(pattern, expression));
            }

            SwitchExpressionSyntax switchExpression = SyntaxFactory.SwitchExpression(switchStatement.Expression, arms);
            AssignmentExpressionSyntax fullAssignment = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, left, switchExpression).WithoutLeadingTrivia();
            ExpressionStatementSyntax fullStatement = SyntaxFactory.ExpressionStatement(fullAssignment).WithoutLeadingTrivia();

            SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            SyntaxNode newRoot = oldRoot.ReplaceNode(switchStatement, fullStatement);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
