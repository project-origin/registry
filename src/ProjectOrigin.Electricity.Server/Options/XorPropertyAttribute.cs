

using System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class XorPropertyAttribute : ValidationAttribute
{
    private readonly string[] _propertyNames;

    public XorPropertyAttribute(params string[] propertyNames)
    {
        _propertyNames = propertyNames;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        Dictionary<string, object?> values = new Dictionary<string, object?>();

        foreach (var propertyName in _propertyNames)
        {
            var property = validationContext.ObjectType.GetProperty(propertyName);
            if (property == null)
                return new ValidationResult($"Unknown property: {propertyName}", new[] { propertyName });

            values.Add(propertyName, property.GetValue(validationContext.ObjectInstance));
        }

        var numberOfPropertiesSet = values.Where(x => x.Value != null).Count();

        if (numberOfPropertiesSet == 1)
        {
            return ValidationResult.Success;
        }
        else
        {
            return new ValidationResult("Exactly only one property must be set.", _propertyNames);

        }
    }
}
