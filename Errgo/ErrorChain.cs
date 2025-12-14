namespace Errgo;

/// <summary>
/// This class acts as a shared reference in a chain of errors.
/// Stores complete Error objects to preserve nested error chains.
/// </summary>
internal sealed class ErrorChain
{
    private readonly List<Error> _errors = [];
    
    public IReadOnlyList<Error> Errors => _errors;
    
    public void Append(Error error)
    {
        if (error) _errors.Add(error);
    }
    
    public void Prepend(Error error)
    {
        if (error) _errors.Insert(0, error);
    }
}
