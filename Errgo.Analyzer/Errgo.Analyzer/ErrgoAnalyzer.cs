using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Errgo.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ErrgoAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ERRGO001";
        private const string Category = "Usage";

        private static readonly LocalizableString Title = "Error not checked";
        private static readonly LocalizableString MessageFormat = "Error variable '{0}' is not checked";
        private static readonly LocalizableString Description = "Error return values from method calls should be checked with 'if (err)' in the next statement.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
            isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics 
            => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.LocalDeclarationStatement);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var declaration = (LocalDeclarationStatementSyntax)context.Node;
            
            foreach (var variable in declaration.Declaration.Variables)
            {
                if (variable.Initializer == null) continue;

                var typeInfo = context.SemanticModel.GetTypeInfo(declaration.Declaration.Type);
                if (!IsErrorType(typeInfo.Type)) continue;

                if (!IsMethodCall(variable.Initializer.Value)) continue;

                var variableName = variable.Identifier.Text;

                if (IsErrorChecked(declaration, variableName)) continue;

                var diagnostic = Diagnostic.Create(Rule, variable.GetLocation(), variableName);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsErrorType(ITypeSymbol type) 
            => type?.Name == "Error" && type.ContainingNamespace?.ToString() == "Errgo";

        private static bool IsMethodCall(ExpressionSyntax expression) 
            => expression is InvocationExpressionSyntax;

        private static bool IsErrorChecked(StatementSyntax statement, string errorVarName)
        {
            var parent = statement.Parent;
            if (parent == null) return false;

            var statements = (parent as BlockSyntax)?.Statements;
            if (statements == null) return false;

            var index = statements.Value.IndexOf(statement);
            if (index < 0) return false;
            
            // If this is the last statement, no check is needed
            if (index >= statements.Value.Count - 1) return true;

            var nextStatement = statements.Value[index + 1];

            // Check if next statement is an if statement that mentions the error variable
            if (nextStatement is IfStatementSyntax ifStatement)
            {
                return ifStatement.Condition
                    .DescendantNodesAndSelf()
                    .OfType<IdentifierNameSyntax>()
                    .Any(id => id.Identifier.Text == errorVarName);
            }

            return false;
        }
    }
}
