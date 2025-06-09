namespace Validation;

public interface IValidator<T>
{
    Dictionary<string, string> Validate(T entity);
}
