using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
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

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId, 
            Title, 
            MessageFormat, 
            Category, 
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true, 
            description: Description,
            helpLinkUri: null);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeLocalDeclaration, SyntaxKind.LocalDeclarationStatement);
            context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
        }

        private static void AnalyzeLocalDeclaration(SyntaxNodeAnalysisContext context)
        {
            var declaration = (LocalDeclarationStatementSyntax)context.Node;
            
            foreach (var variable in declaration.Declaration.Variables)
            {
                if (variable.Initializer == null) continue;

                var typeInfo = context.SemanticModel.GetTypeInfo(declaration.Declaration.Type);
                if (IsErrorType(typeInfo.Type))
                {
                    // Skip if it's a direct error instantiation (new Error(...))
                    var initializerExpression = variable.Initializer.Value;
                    if (IsDirectErrorInstantiation(initializerExpression))
                    {
                        continue; // Don't check errors that are explicitly created
                    }

                    CheckErrorVariable(context, variable.Identifier.Text, variable, declaration);
                }
            }
        }

        private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            var assignment = (AssignmentExpressionSyntax)context.Node;

            // Handle tuple deconstruction: (var x, var err) = ...
            if (assignment.Left is TupleExpressionSyntax tupleExpression)
            {
                AnalyzeTupleDeconstruction(context, assignment, tupleExpression);
                return;
            }

            // Handle simple assignment: err = ...
            if (assignment.Left is IdentifierNameSyntax identifier)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(identifier);
                if (symbolInfo.Symbol is ILocalSymbol localSymbol && IsErrorType(localSymbol.Type))
                {
                    var statement = assignment.FirstAncestorOrSelf<StatementSyntax>();
                    if (statement != null)
                    {
                        CheckErrorVariable(context, identifier.Identifier.Text, identifier, statement);
                    }
                }
            }
        }

        private static void AnalyzeTupleDeconstruction(SyntaxNodeAnalysisContext context, AssignmentExpressionSyntax assignment, TupleExpressionSyntax tupleExpression)
        {
            var errorVariables = new List<ErrorVariable>();
            
            foreach (var argument in tupleExpression.Arguments)
            {
                if (argument.Expression is DeclarationExpressionSyntax declarationExpression)
                {
                    var typeInfo = context.SemanticModel.GetTypeInfo(declarationExpression.Type);
                    if (IsErrorType(typeInfo.Type))
                    {
                        if (declarationExpression.Designation is SingleVariableDesignationSyntax designation)
                        {
                            errorVariables.Add(new ErrorVariable(designation.Identifier.Text, designation));
                        }
                    }
                }
                else
                {
                    if (argument.Expression is IdentifierNameSyntax identifier)
                    {
                        var symbolInfo = context.SemanticModel.GetSymbolInfo(identifier);
                        if (symbolInfo.Symbol is ILocalSymbol localSymbol && IsErrorType(localSymbol.Type))
                        {
                            errorVariables.Add(new ErrorVariable(identifier.Identifier.Text, identifier));
                        }
                    }
                }
            }

            if (errorVariables.Count == 0) return;

            var statement = assignment.FirstAncestorOrSelf<StatementSyntax>();
            if (statement == null) return;

            foreach (var errorVar in errorVariables)
            {
                CheckErrorVariable(context, errorVar.Name, errorVar.Node, statement);
            }
        }

        private static void CheckErrorVariable(SyntaxNodeAnalysisContext context, string errorVarName, SyntaxNode errorNode, StatementSyntax statement)
        {
            var parentBlock = statement.Parent;
            if (parentBlock == null) return;

            var statements = GetStatements(parentBlock);
            if (statements == null) return;

            var currentIndex = statements.IndexOf(statement);
            if (currentIndex < 0 || currentIndex >= statements.Count - 1) return;

            var nextStatement = statements[currentIndex + 1];

            if (!IsErrorCheckedInIfStatement(nextStatement, errorVarName))
            {
                var diagnostic = Diagnostic.Create(Rule, errorNode.GetLocation(), errorVarName);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsDirectErrorInstantiation(ExpressionSyntax expression)
        {
            // Check for new Error(...)
            if (expression is ObjectCreationExpressionSyntax objectCreation)
            {
                var typeName = objectCreation.Type.ToString();
                return typeName == "Error" || typeName == "Errgo.Error";
            }

            // Check for Error.None or Error.Empty
            if (expression is MemberAccessExpressionSyntax memberAccess)
            {
                var leftType = memberAccess.Expression.ToString();
                var memberName = memberAccess.Name.ToString();

                if ((leftType == "Error" || leftType == "Errgo.Error") &&
                    (memberName == "None" || memberName == "Empty"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsErrorType(ITypeSymbol type)
        {
            if (type == null) return false;
            if (type.Name != "Error") return false;

            var namespaceSymbol = type.ContainingNamespace;
            if (namespaceSymbol == null) return false;

            return namespaceSymbol.ToString() == "Errgo";
        }

        private static IList<StatementSyntax> GetStatements(SyntaxNode node)
        {
            if (node is BlockSyntax block) 
                return block.Statements.ToList();

            if (node is SwitchSectionSyntax switchSection) 
                return switchSection.Statements.ToList();

            return null;
        }

        private static bool IsErrorCheckedInIfStatement(StatementSyntax statement, string errorVarName)
        {
            if (statement is IfStatementSyntax ifStatement)
                return IsErrorCheckedInExpression(ifStatement.Condition, errorVarName);

            return false;
        }

        private static bool IsErrorCheckedInExpression(ExpressionSyntax expression, string errorVarName)
        {
            // Direct identifier check: if (err)
            if (expression is IdentifierNameSyntax identifier && identifier.Identifier.Text == errorVarName)
            {
                return true;
            }

            // Prefix unary (logical not): if (!err)
            if (expression is PrefixUnaryExpressionSyntax prefixUnary && prefixUnary.IsKind(SyntaxKind.LogicalNotExpression))
            {
                return IsErrorCheckedInExpression(prefixUnary.Operand, errorVarName);
            }

            // Binary expressions: if (err != null), if (err == Error.None), etc.
            if (expression is BinaryExpressionSyntax binary)
            {
                var leftMentioned = binary.Left.DescendantNodesAndSelf()
                    .OfType<IdentifierNameSyntax>()
                    .Any(id => id.Identifier.Text == errorVarName);

                var rightMentioned = binary.Right.DescendantNodesAndSelf()
                    .OfType<IdentifierNameSyntax>()
                    .Any(id => id.Identifier.Text == errorVarName);

                return leftMentioned || rightMentioned;
            }

            // Parenthesized expression
            if (expression is ParenthesizedExpressionSyntax parenthesized)
            {
                return IsErrorCheckedInExpression(parenthesized.Expression, errorVarName);
            }

            // Logical AND/OR expressions
            if (expression.IsKind(SyntaxKind.LogicalAndExpression) || 
                expression.IsKind(SyntaxKind.LogicalOrExpression))
            {
                if (expression is BinaryExpressionSyntax logicalBinary)
                {
                    return IsErrorCheckedInExpression(logicalBinary.Left, errorVarName) ||
                           IsErrorCheckedInExpression(logicalBinary.Right, errorVarName);
                }
            }

            return false;
        }

        private readonly struct ErrorVariable
        {
            public string Name { get; }
            public SyntaxNode Node { get; }

            public ErrorVariable(string name, SyntaxNode node)
            {
                Name = name;
                Node = node;
            }
        }
    }
}
