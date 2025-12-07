namespace Errgo;

/// <summary>
/// Internal chain for error accumulation.
/// This class acts as a shared reference that can be mutated across Error struct copies.
/// Stores complete Error values to preserve nested error chains.
/// </summary>
internal sealed class ErrorChain
{
    private readonly List<Error> _errors = new List<Error>();
    
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
