using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace MakeSealed
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MakeSealedAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MakeSealed";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Class Modifier:";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        private static HashSet<string> inheritedFrom = new HashSet<string>();

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // this Action will add all classes that are inherited from to inheritedFrom
            context.RegisterSyntaxNodeAction(DetermineInheritance, SyntaxKind.ClassDeclaration);

            // this action will recommend or not recommend sealing based on numerous criteria
            context.RegisterSyntaxNodeAction(RecommendSealing, SyntaxKind.ClassDeclaration);
        }

        private static void DetermineInheritance(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;

            if (!(classDeclaration.BaseList is null))
            {
                //add each type this class inherits from to inheritedFrom hash set
                foreach (BaseTypeSyntax baseType in classDeclaration.BaseList.Types)
                {
                    inheritedFrom.Add(baseType.Type.ToString());
                }
            }
        }

        private static void RecommendSealing(SyntaxNodeAnalysisContext context)
        {
            var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

            bool isNotSealed = classDeclarationSyntax.Modifiers.Where(mod => mod.IsKind(SyntaxKind.SealedKeyword)).Count() == 0;
            bool isNotAbstract = classDeclarationSyntax.Modifiers.Where(mod => mod.IsKind(SyntaxKind.AbstractKeyword)).Count() == 0;
            bool isNotStatic = classDeclarationSyntax.Modifiers.Where(mod => mod.IsKind(SyntaxKind.StaticKeyword)).Count() == 0;
            bool hasVirtualMembers = classDeclarationSyntax.Members
                .Where(
                    member => member.IsKind(SyntaxKind.MethodDeclaration) 
                    && ((MethodDeclarationSyntax)member).Modifiers
                        .Where(mod => mod.IsKind(SyntaxKind.VirtualKeyword))
                        .Any()
                ).Any();

            //if class is not already sealed, abstract, static, 
            if (isNotSealed 
                && isNotAbstract 
                && isNotStatic 
                && !inheritedFrom.Contains(classDeclarationSyntax.Identifier.ToString())
                && !hasVirtualMembers)
            {
                var diagnostic = Diagnostic.Create(Rule, classDeclarationSyntax.GetLocation(), $"Consider sealing class {classDeclarationSyntax.Identifier}");
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
