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
        private static readonly LocalizableString Description = "Error variables should be checked with 'if (err)', 'if (err != Error.None)' or similar before being used or before the next statement.";

        private static readonly DiagnosticDescriptor Rule001 = new DiagnosticDescriptor(
            DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
            isEnabledByDefault: true, description: Description, helpLinkUri: null);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics 
            => ImmutableArray.Create(Rule001);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register for both declaration and assignment statements
            // We check if the right-hand side is a function call returning Error
            context.RegisterSyntaxNodeAction(
                AnalyzeNode, 
                SyntaxKind.LocalDeclarationStatement,
                SyntaxKind.SimpleAssignmentExpression);
        }

        /// <summary>
        /// Analyzes declarations and assignments to find unchecked Error variables.
        /// Only flags Errors from function calls (not new Error(...) or Error.None).
        /// </summary>
        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            // Get the right-hand side expression and the variables being assigned
            var (rhsExpression, errorVariables) = ExtractErrorVariables(context);
            
            if (rhsExpression == null || errorVariables.Count == 0) return;

            // Skip if developer explicitly created the error (not from a function)
            if (IsDirectErrorInstantiation(rhsExpression)) return;

            // Find the statement containing this assignment/declaration
            var statement = context.Node.FirstAncestorOrSelf<StatementSyntax>();
            if (statement == null) return;

            // Ensure each Error variable is checked
            foreach (var (varName, varNode) in errorVariables)
            {
                EnsureErrorChecked(context, varName, varNode, statement);
            }
        }

        /// <summary>
        /// Extracts Error variables from declarations or assignments.
        /// Returns the RHS expression and list of (variableName, syntaxNode) tuples.
        /// </summary>
        private static (ExpressionSyntax, System.Collections.Generic.List<(string, SyntaxNode)>) ExtractErrorVariables(
            SyntaxNodeAnalysisContext context)
        {
            var errorVars = new System.Collections.Generic.List<(string, SyntaxNode)>();

            // Handle: var err = GetError(), Error err = GetError(), var (x, err) = GetData()
            if (context.Node is LocalDeclarationStatementSyntax declaration)
            {
                foreach (var variable in declaration.Declaration.Variables)
                {
                    if (variable.Initializer == null) continue;

                    var typeInfo = context.SemanticModel.GetTypeInfo(declaration.Declaration.Type);
                    if (IsErrgoError(typeInfo.Type))
                    {
                        errorVars.Add((variable.Identifier.Text, variable));
                        return (variable.Initializer.Value, errorVars);
                    }
                }
            }

            // Handle: err = GetError(), (var x, err) = GetData(), (x, err) = GetData()
            if (context.Node is AssignmentExpressionSyntax assignment)
            {
                // Simple assignment: err = GetError()
                if (assignment.Left is IdentifierNameSyntax identifier)
                {
                    var symbolInfo = context.SemanticModel.GetSymbolInfo(identifier);
                    if (symbolInfo.Symbol is ILocalSymbol localSymbol && IsErrgoError(localSymbol.Type))
                    {
                        errorVars.Add((identifier.Identifier.Text, identifier));
                        return (assignment.Right, errorVars);
                    }
                }

                // Tuple: (var x, err) = GetData() or (x, err) = GetData()
                if (assignment.Left is TupleExpressionSyntax tuple)
                {
                    foreach (var arg in tuple.Arguments)
                    {
                        // New variable: var err
                        if (arg.Expression is DeclarationExpressionSyntax declExpr)
                        {
                            var typeInfo = context.SemanticModel.GetTypeInfo(declExpr.Type);
                            if (IsErrgoError(typeInfo.Type) && 
                                declExpr.Designation is SingleVariableDesignationSyntax designation)
                            {
                                errorVars.Add((designation.Identifier.Text, designation));
                            }
                        }
                        // Existing variable: err
                        else if (arg.Expression is IdentifierNameSyntax ident)
                        {
                            var symbolInfo = context.SemanticModel.GetSymbolInfo(ident);
                            if (symbolInfo.Symbol is ILocalSymbol localSym && IsErrgoError(localSym.Type))
                            {
                                errorVars.Add((ident.Identifier.Text, ident));
                            }
                        }
                    }

                    if (errorVars.Count > 0)
                    {
                        return (assignment.Right, errorVars);
                    }
                }
            }

            return (null, errorVars);
        }

        private static void EnsureErrorChecked(
            SyntaxNodeAnalysisContext context, 
            string errorVarName, 
            SyntaxNode errorNode, 
            StatementSyntax statement)
        {
            var parentBlock = statement.Parent;
            if (parentBlock == null) return;

            var statements = (parentBlock as BlockSyntax)?.Statements.ToList() ?? 
                             (parentBlock as SwitchSectionSyntax)?.Statements.ToList();
            if (statements == null) return;

            var currentIndex = statements.IndexOf(statement);
            if (currentIndex < 0 || currentIndex >= statements.Count - 1) return;

            var nextStatement = statements[currentIndex + 1];

            // Check if next statement is: if (err) or if (!err) or if (err == Error.None), etc.
            if (nextStatement is IfStatementSyntax ifStatement && 
                IsErrorMentioned(ifStatement.Condition, errorVarName))
            {
                return;
            }

            // Error not checked - report diagnostic
            var diagnostic = Diagnostic.Create(Rule001, errorNode.GetLocation(), errorVarName);
            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsDirectErrorInstantiation(ExpressionSyntax expression)
        {
            // new Error(...)
            if (expression is ObjectCreationExpressionSyntax objectCreation)
            {
                var typeName = objectCreation.Type.ToString();
                return typeName == "Error" || typeName == "Errgo.Error";
            }

            // Error.None or Error.Empty
            if (expression is MemberAccessExpressionSyntax memberAccess)
            {
                var leftType = memberAccess.Expression.ToString();
                var memberName = memberAccess.Name.ToString();
                return (leftType == "Error" || leftType == "Errgo.Error") &&
                       (memberName == "None" || memberName == "Empty");
            }

            return false;
        }

        private static bool IsErrgoError(ITypeSymbol type)
        {
            return type != null && 
                   type.Name == "Error" && 
                   type.ContainingNamespace?.ToString() == "Errgo";
        }

        private static bool IsErrorMentioned(ExpressionSyntax expression, string errorVarName)
        {
            // if (err) or if (!err)
            if (expression is IdentifierNameSyntax identifier && 
                identifier.Identifier.Text == errorVarName)
            {
                return true;
            }

            if (expression is PrefixUnaryExpressionSyntax prefixUnary && 
                prefixUnary.IsKind(SyntaxKind.LogicalNotExpression))
            {
                return IsErrorMentioned(prefixUnary.Operand, errorVarName);
            }

            // if (err == ...) or if (err != ...)
            if (expression is BinaryExpressionSyntax binary)
            {
                return binary.DescendantNodesAndSelf()
                    .OfType<IdentifierNameSyntax>()
                    .Any(id => id.Identifier.Text == errorVarName);
            }

            // if ((err))
            if (expression is ParenthesizedExpressionSyntax parenthesized)
            {
                return IsErrorMentioned(parenthesized.Expression, errorVarName);
            }

            return false;
        }
    }
}
