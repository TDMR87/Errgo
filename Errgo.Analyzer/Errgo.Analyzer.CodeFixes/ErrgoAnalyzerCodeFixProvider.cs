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

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the statement containing the unchecked error variable
            var statement = root.FindToken(diagnosticSpan.Start)
                .Parent
                .AncestorsAndSelf()
                .OfType<StatementSyntax>()
                .FirstOrDefault();

            if (statement == null) return;

            // Get the error variable name from the diagnostic message
            // Message format: "Error variable '{0}' is not checked"
            var errorVarName = diagnostic.Properties.ContainsKey("ErrorVariableName") 
                ? diagnostic.Properties["ErrorVariableName"] 
                : diagnostic.Descriptor.MessageFormat.ToString()
                    .Split('\'')
                    .Skip(1)
                    .FirstOrDefault() ?? "err";

            // Register code fix to add error check
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: c => AddErrorCheckAsync(context.Document, statement, errorVarName, c),
                    equivalenceKey: Title),
                diagnostic);
        }

        private async Task<Document> AddErrorCheckAsync(
            Document document, 
            StatementSyntax statement, 
            string errorVarName, 
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null) return document;

            // Create the if statement: if (err) { }
            var errorIdentifier = SyntaxFactory.IdentifierName(errorVarName);
            
            var ifStatement = SyntaxFactory.IfStatement(
                condition: errorIdentifier,
                statement: SyntaxFactory.Block()  // Empty block
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
    }
}
