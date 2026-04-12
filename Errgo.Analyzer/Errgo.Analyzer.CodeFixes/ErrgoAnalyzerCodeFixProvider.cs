using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Errgo.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ErrgoAnalyzerCodeFixProvider)), Shared]
    public class ErrgoAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Add error check";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(ErrgoAnalyzer.DiagnosticId); }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            // Find the Errgo analyzer diagnostic by it's ID
            var diagnostic = context.Diagnostics.FirstOrDefault(d => d.Id == ErrgoAnalyzer.DiagnosticId);
            if (diagnostic == null) return;

            // Find the statement containing the unchecked error variable
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var statement = root.FindToken(diagnosticSpan.Start)
                .Parent
                .AncestorsAndSelf()
                .OfType<StatementSyntax>()
                .FirstOrDefault();

            if (statement == null) return;

            // The variable name is passed from the analyzer via Properties.
            if (!diagnostic.Properties.TryGetValue(ErrgoAnalyzer.ErrorVariableNameKey, 
                out var errorVarName) || errorVarName == null)
            {
                return;
            }

            // Register a code fix suggestion to add an error check after the statement
            context.RegisterCodeFix(
                diagnostic: diagnostic,
                action: CodeAction.Create(
                    title: Title,
                    equivalenceKey: Title,
                    createChangedDocument: cancellationToken => AddErrorCheckAsync(
                        context.Document, 
                        statement, 
                        errorVarName, 
                        cancellationToken)
                    ));
        }

        /// <summary>
        /// Inserts an error-checking if statement after the specified statement in the provided document's syntax tree.
        /// </summary>
        /// <param name="document">The document to update with the inserted error-checking statement.</param>
        /// <param name="statement">The statement after which the error-checking if statement will be inserted.</param>
        /// <param name="errorVarName">The name of the variable to use as the condition in the error-checking if statement.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A new document with the error-checking if statement inserted after the specified statement.</returns>
        private async Task<Document> AddErrorCheckAsync(
            Document document, 
            StatementSyntax statement, 
            string errorVarName, 
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null) return document;
            
            var errorIdentifierNode = SyntaxFactory.IdentifierName(errorVarName);

            // Create the if statement: if (err) { }
            // Note: this relies on the implicit bool operator defined on Errgo.Error:
            // Without it, using a struct directly as an if-condition would be a compile error.
            var ifStatement = SyntaxFactory.IfStatement(
                condition: errorIdentifierNode,
                statement: SyntaxFactory.Block()
            ).WithLeadingTrivia(statement.GetLeadingTrivia())
             .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

            // Find the parent block and the statement's position
            var parentBlock = statement.Parent as BlockSyntax;
            if (parentBlock == null) return document;
            var statementIndex = parentBlock.Statements.IndexOf(statement);
            if (statementIndex < 0) return document;

            // Insert the if statement after the current statement
            var newStatements = parentBlock.Statements.Insert(statementIndex + 1, ifStatement);
            var newBlock = parentBlock.WithStatements(newStatements);

            var newRoot = root.ReplaceNode(parentBlock, newBlock);
            return document.WithSyntaxRoot(newRoot);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md#built-in-fixallprovider
            return WellKnownFixAllProviders.BatchFixer;
        }
    }
}
