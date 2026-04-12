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

        /// <summary>
        /// Describes the diagnostic raised by this analyzer
        /// </summary>
        private static readonly DiagnosticDescriptor ErrgoDiagnosticDescriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Error not checked",
            messageFormat: "Error variable '{0}' is not checked",
            description: "Error return values from method calls should be checked with 'if (err)' in the next statement.",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>
        /// Gets a new empty dictionary of string keys and values for storing diagnostic properties. 
        /// Used to pass data from the analyzer to the code fix provider.
        /// </summary>
        private static ImmutableDictionary<string, string> DiagnosticProperties 
            => ImmutableDictionary<string, string>.Empty;

        /// <summary>
        /// Gets the set of diagnostic descriptors supported by this analyzer.
        /// </summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics 
            => ImmutableArray.Create(ErrgoDiagnosticDescriptor);

        /// <summary>
        /// Initializes the analyzer and registers actions to be executed during code analysis.
        /// </summary>
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeLocalErrorDeclaration, SyntaxKind.LocalDeclarationStatement);
            context.RegisterSyntaxNodeAction(AnalyzeTupleDeconstruction, SyntaxKind.SimpleAssignmentExpression);
        }

        private static void AnalyzeLocalErrorDeclaration(SyntaxNodeAnalysisContext context)
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

                var diagnostic = Diagnostic.Create(
                    descriptor: ErrgoDiagnosticDescriptor, 
                    location: variable.Identifier.GetLocation(), 
                    properties: DiagnosticProperties.Add(ErrorVariableNameKey, variableName), 
                    messageArgs: variableName);

                context.ReportDiagnostic(diagnostic);
            }
        }

        private static void AnalyzeTupleDeconstruction(SyntaxNodeAnalysisContext context)
        {
            var assignment = (AssignmentExpressionSyntax)context.Node;
            
            // Check if right side is a method call
            if (!IsMethodCall(assignment.Right)) return;

            // Find the statement containing this assignment
            var statement = assignment.FirstAncestorOrSelf<StatementSyntax>();
            if (statement == null) return;

            // Handle: (var str, var err) = GetData()
            if (assignment.Left is TupleExpressionSyntax tupleExpression)
            {
                AnalyzeTupleElements(context, tupleExpression.Arguments, statement);
            }

            // Handle: var (str, err) = GetData()
            else if (assignment.Left is DeclarationExpressionSyntax declExpr && 
                     declExpr.Designation is ParenthesizedVariableDesignationSyntax parenDesignation)
            {
                AnalyzeParenthesizedDesignation(context, parenDesignation, statement);
            }
        }

        private static void AnalyzeTupleElements(
            SyntaxNodeAnalysisContext context,
            SeparatedSyntaxList<ArgumentSyntax> arguments,
            StatementSyntax statement)
        {
            foreach (var argument in arguments)
            {
                string variableName = null;
                SyntaxNode variableNode = null;

                // Handle: (var err, ...)
                if (argument.Expression is DeclarationExpressionSyntax declExpr)
                {
                    var typeInfo = context.SemanticModel.GetTypeInfo(declExpr.Type);
                    if (!IsErrorType(typeInfo.Type)) continue;

                    if (declExpr.Designation is SingleVariableDesignationSyntax designation)
                    {
                        variableName = designation.Identifier.Text;
                        variableNode = designation;
                    }
                }

                // Handle: (err, ...) where err is already declared elsewhere
                else if (argument.Expression is IdentifierNameSyntax identifier)
                {
                    var symbolInfo = context.SemanticModel.GetSymbolInfo(identifier);
                    if (symbolInfo.Symbol is ILocalSymbol localSymbol && IsErrorType(localSymbol.Type))
                    {
                        variableName = identifier.Identifier.Text;
                        variableNode = identifier;
                    }
                }

                if (variableName != null && variableNode != null)
                {
                    if (!IsErrorChecked(statement, variableName))
                    {
                        var diagnostic = Diagnostic.Create(
                            descriptor: ErrgoDiagnosticDescriptor, 
                            location: variableNode.GetLocation(), 
                            properties: DiagnosticProperties.Add(ErrorVariableNameKey, variableName), 
                            messageArgs: variableName);

                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private static void AnalyzeParenthesizedDesignation(
            SyntaxNodeAnalysisContext context,
            ParenthesizedVariableDesignationSyntax designation,
            StatementSyntax statement)
        {
            foreach (var variable in designation.Variables)
            {
                if (variable is SingleVariableDesignationSyntax singleVar)
                {
                    var symbol = context.SemanticModel.GetDeclaredSymbol(singleVar);
                    if (symbol is ILocalSymbol localSymbol && IsErrorType(localSymbol.Type))
                    {
                        var variableName = singleVar.Identifier.Text;
                        if (!IsErrorChecked(statement, variableName))
                        {
                            var diagnostic = Diagnostic.Create(
                                descriptor: ErrgoDiagnosticDescriptor, 
                                location: singleVar.GetLocation(), 
                                properties: DiagnosticProperties.Add(ErrorVariableNameKey, variableName), 
                                messageArgs: variableName);

                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }

        private static bool IsErrorType(ITypeSymbol type) 
            => type?.Name == "Error" && type.ContainingNamespace?.ToString() == "Errgo";

        private static bool IsMethodCall(ExpressionSyntax expression)
        {
            // Direct method call: GetError()
            if (expression is InvocationExpressionSyntax)
                return true;
            
            // Awaited method call: await GetError()
            if (expression is AwaitExpressionSyntax awaitExpr && 
                awaitExpr.Expression is InvocationExpressionSyntax)
                return true;
            
            return false;
        }

        private static bool IsErrorChecked(StatementSyntax statement, string errorVarName)
        {
            var parent = statement.Parent;
            if (parent == null) return false;

            var statements = (parent as BlockSyntax)?.Statements;
            if (statements == null) return false;

            var index = statements.Value.IndexOf(statement);
            if (index < 0) return false;
            
            // If this is the last statement, only allow it if we're in a void method or constructor
            if (index >= statements.Value.Count - 1)
            {
                var containingMethod = statement.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>();
                if (containingMethod is MethodDeclarationSyntax methodDecl)
                {
                    // Allow if void method
                    return methodDecl.ReturnType.ToString() == "void";
                }
                // Allow for constructors
                if (containingMethod is ConstructorDeclarationSyntax)
                {
                    return true;
                }
                // Otherwise warn (e.g., in methods that return Error)
                return false;
            }

            var nextStatement = statements.Value[index + 1];

            // Check if next statement is an if statement that mentions the error variable
            if (nextStatement is IfStatementSyntax ifStatement)
            {
                return ifStatement.Condition
                    .DescendantNodesAndSelf()
                    .OfType<IdentifierNameSyntax>()
                    .Any(id => id.Identifier.Text == errorVarName);
            }

            // Check if next statement is a return statement that returns the error
            if (nextStatement is ReturnStatementSyntax returnStatement)
            {
                return returnStatement.Expression
                    ?.DescendantNodesAndSelf()
                    .OfType<IdentifierNameSyntax>()
                    .Any(id => id.Identifier.Text == errorVarName) ?? false;
            }

            return false;
        }

        public const string ErrorVariableNameKey = "ErrorVariableName";
    }
}
