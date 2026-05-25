namespace SistemaEvaluacionAcademica.Domain.Exceptions;

public class ConcurrencyException : DomainException
{
    public ConcurrencyException(string message) : base(message) { }
}
