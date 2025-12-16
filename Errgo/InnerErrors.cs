namespace Errgo;

/// <summary>
/// This class acts as a shared reference in a chain of errors.
/// Stores complete Error objects to preserve nested error chains.
/// </summary>
internal sealed class InnerErrors
{
    private Error[] _errors = [];

    public Error[] Errors => _errors;
    
    public void Append(Error error)
    {
        if (!error) return;
        var newArray = new Error[_errors.Length + 1];
        Array.Copy(_errors, newArray, _errors.Length);
        newArray[_errors.Length] = error;
        _errors = newArray;
    }
    
    public void Prepend(Error error)
    {
        if (!error) return;
        var newArray = new Error[_errors.Length + 1];
        newArray[0] = error;
        Array.Copy(_errors, 0, newArray, 1, _errors.Length);
        _errors = newArray;
    }
}
