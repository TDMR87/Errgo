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

        private static readonly DiagnosticDescriptor Rule001 = new DiagnosticDescriptor(
            DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
            isEnabledByDefault: true, description: Description, helpLinkUri: null);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics 
            => ImmutableArray.Create(Rule001);

        public override void Initialize(AnalysisContext context)
        {
            // Don't analyze generated code
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            // Analyzer actions should be able to run concurrently
            context.EnableConcurrentExecution();

            // New variables declaration:
            // e.g. var (someVal, err) = ...
            context.RegisterSyntaxNodeAction(
                AnalyzeDeclaration, 
                SyntaxKind.LocalDeclarationStatement);

            // Existing variable assignment:
            // e.g. (var someVal, err) = ...
            context.RegisterSyntaxNodeAction(
                AnalyzeAssignment, 
                SyntaxKind.SimpleAssignmentExpression);
        }

        /// <summary>
        /// Analyzes local variable declarations (new variables)
        /// Handles: Error err = ..., var err = ..., var (x, err) = ...
        /// </summary>
        private static void AnalyzeDeclaration(SyntaxNodeAnalysisContext context)
        {
            // Access declaration details
            var declaration = (LocalDeclarationStatementSyntax)context.Node;
            
            // Loop through each variable in the declaration
            foreach (var variable in declaration.Declaration.Variables)
            {
                // Get type information for the declared variable
                var typeInfo = context.SemanticModel.GetTypeInfo(declaration.Declaration.Type);

                // Only interested in our Error type
                if (!IsErrgoError(typeInfo.Type)) continue;

                // Skip variables without initialization (e.g., Error err;)
                if (variable.Initializer == null) continue;

                // Get the right-hand side of the assignment
                // Example: For "var err = new Error(...)", this gets "new Error(...)"
                var initializerExpression = variable.Initializer.Value;
                    
                // Skip if errors are explicitly created by the developer,
                // we don't need to check: var err = new Error("msg"), var err = Error.None
                // We DO need to check method return values,
                // e.g. var err = GetError(), var err = repo.FindUser()
                if (IsDirectErrorInstantiation(initializerExpression)) continue;

                // If we got this far, this is an Error return value from a method call
                EnsureErrorVariableChecked(context, variable.Identifier.Text, variable, declaration);
            }
        }

        /// <summary>
        /// Analyzes assignment expressions (to existing variables)
        /// Handles: err = ..., (var x, err) = ..., (x, err) = ...
        /// </summary>
        private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            // Access assignment details
            var assignment = (AssignmentExpressionSyntax)context.Node;

            // Handle tuple deconstruction assignments
            // Examples: (var x, var err) = GetData(), (x, err) = GetData()
            if (assignment.Left is TupleExpressionSyntax tupleExpression)
            {
                AnalyzeTupleDeconstruction(context, assignment, tupleExpression);
                return;
            }

            // Handle simple assignment to existing Error variable
            // Example: err = GetError()
            if (assignment.Left is IdentifierNameSyntax identifier)
            {
                // Get symbol information to check the variable's type
                var symbolInfo = context.SemanticModel.GetSymbolInfo(identifier);
                
                // Only proceed if it's a local variable of type Errgo.Error
                if (symbolInfo.Symbol is ILocalSymbol localSymbol && IsErrgoError(localSymbol.Type))
                {
                    // Find the statement containing this assignment
                    // Example: For "err = GetError();", this gets the entire statement
                    var statement = assignment.FirstAncestorOrSelf<StatementSyntax>();
                    if (statement != null)
                    {
                        // Ensure this error is checked in the next statement
                        EnsureErrorVariableChecked(context, identifier.Identifier.Text, identifier, statement);
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes tuple deconstruction to find Error variables that need checking.
        /// Handles both new and existing variables: (var x, var err) = ..., (x, err) = ..., (var x, err) = ...
        /// </summary>
        private static void AnalyzeTupleDeconstruction(SyntaxNodeAnalysisContext context, AssignmentExpressionSyntax assignment, TupleExpressionSyntax tupleExpression)
        {
            // Collect all Error variables found in the tuple
            var errorVariables = new List<ErrorVariable>();
            
            // Loop through each element in the tuple
            // Example: For "(var x, err)", this loops through "var x" and "err"
            foreach (var argument in tupleExpression.Arguments)
            {
                // Check if this is a new variable declaration (has 'var' keyword)
                // Example: "var err" in (var x, var err) = GetData()
                if (argument.Expression is DeclarationExpressionSyntax declarationExpression)
                {
                    // Get the type of this declared variable
                    var typeInfo = context.SemanticModel.GetTypeInfo(declarationExpression.Type);
                    
                    if (IsErrgoError(typeInfo.Type) && 
                        declarationExpression.Designation is SingleVariableDesignationSyntax designation)
                    {
                        errorVariables.Add(new ErrorVariable(designation.Identifier.Text, designation));
                    }
                }
                // Check if this is an existing variable being reassigned
                // Example: "err" in (x, err) = GetData() where err already exists
                else if (argument.Expression is IdentifierNameSyntax identifier)
                {
                    // Get symbol information to check the variable's type
                    var symbolInfo = context.SemanticModel.GetSymbolInfo(identifier);
                    
                    // Is it a local variable of type Errgo.Error?
                    if (symbolInfo.Symbol is ILocalSymbol localSymbol && IsErrgoError(localSymbol.Type))
                    {
                        errorVariables.Add(new ErrorVariable(identifier.Identifier.Text, identifier));
                    }
                }
            }

            // No Error variables found in this tuple? Nothing to check
            if (errorVariables.Count == 0) return;

            // Find the statement containing this assignment
            var statement = assignment.FirstAncestorOrSelf<StatementSyntax>();
            if (statement == null) return;

            // Ensure each Error variable is checked in the next statement
            foreach (var errorVar in errorVariables)
            {
                EnsureErrorVariableChecked(context, errorVar.Name, errorVar.Node, statement);
            }
        }

        private static void EnsureErrorVariableChecked(SyntaxNodeAnalysisContext context, string errorVarName, SyntaxNode errorNode, StatementSyntax statement)
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
                var diagnostic = Diagnostic.Create(Rule001, errorNode.GetLocation(), errorVarName);
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

                return 
                    ((leftType == "Error" || leftType == "Errgo.Error") &&
                    (memberName == "None" || memberName == "Empty"));
            }

            return false;
        }

        private static bool IsErrgoError(ITypeSymbol type)
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
            {
                return IsErrorCheckedInExpression(ifStatement.Condition, errorVarName);
            }

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
