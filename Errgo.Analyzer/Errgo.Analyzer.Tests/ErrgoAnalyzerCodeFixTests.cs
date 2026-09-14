namespace Errgo.Analyzer.Tests;

public class ErrgoAnalyzerCodeFixTests
{
    [Fact]
    public async Task CodeFix_FailsToApply_TopLevelStatements()
    {
        var testCode = /* lang=c#-test */
            """
            using Errgo;

            Error GetError() => new Error();

            var [|err|] = GetError();
            """;

        var codeFix = /* lang=c#-test */
            """
            using Errgo;

            Error GetError() => new Error();

            var err = GetError();
            if (err)
            {
            }
            """;

        await ErrgoVerifier.VerifyCodeFixAsync(testCode, codeFix);
    }
}