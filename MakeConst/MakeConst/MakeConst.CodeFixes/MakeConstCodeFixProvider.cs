using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Formatting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MakeConst
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeConstCodeFixProvider)), Shared]
    public class MakeConstCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(MakeConstAnalyzer.DiagnosticId); }
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
                    createChangedDocument: c => MakeSwitchExpressionAsync(context.Document, declaration, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private static async Task<Document> MakeSwitchExpressionAsync(Document document, SwitchStatementSyntax switchStatement, CancellationToken cancellationToken)
        {
            // extract governing expression (this should be easy)
            // create list of arms
            // create arm with a PatternSyntax and an ExpressionSyntax
            // ExpressionSyntax --> LiteralExpressionSyntax --> NumericLiteralExpression
            // create switch expression with governing expression and list of arms
            //SwitchExpressionSyntax = SyntaxFactory.SwitchExpression()
            //SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            //SyntaxNode newRoot = oldRoot.ReplaceNode(switchStatement, switchStatement.Sections.First());
            return document;//.WithSyntaxRoot(newRoot);
        }

        private static async Task<Document> MakeConstAsync(Document document, AccessorDeclarationSyntax accessorDeclaration, CancellationToken cancellationToken)
        {
            // Remove the leading trivia from the local declaration.
            SyntaxToken firstToken = accessorDeclaration.GetFirstToken();
            SyntaxTriviaList leadingTrivia = firstToken.LeadingTrivia;
            AccessorDeclarationSyntax trimmedDeclaration = accessorDeclaration.ReplaceToken(
                firstToken, SyntaxFactory.Token(SyntaxKind.InitKeyword));

            // Create a const token with the leading trivia.
            //SyntaxToken initToken = SyntaxFactory.Token(leadingTrivia, SyntaxKind.InitKeyword, SyntaxFactory.TriviaList(SyntaxFactory.ElasticMarker));

            // Insert the const token into the modifiers list, creating a new modifiers list.
            //SyntaxTokenList newModifiers = new SyntaxTokenList(initToken);//trimmedDeclaration.Modifiers.Replace(initToken);//.Insert(0, initToken);
            // Produce the new local declaration.
            AccessorDeclarationSyntax newLocal = trimmedDeclaration;//.WithModifiers(newModifiers);

            // Add an annotation to format the new local declaration.
            AccessorDeclarationSyntax formattedLocal = newLocal.WithAdditionalAnnotations(Formatter.Annotation);

            // Replace the old local declaration with the new local declaration.
            SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            SyntaxNode newRoot = oldRoot.ReplaceNode(accessorDeclaration, formattedLocal);

            int x = 0;
            switch(x)
            { 
                case 0:
                    break;
            }

            // Return document with transformed tree.
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
