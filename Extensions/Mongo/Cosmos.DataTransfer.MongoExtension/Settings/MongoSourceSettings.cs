using System.ComponentModel.DataAnnotations;
using Cosmos.DataTransfer.Interfaces.Manifest;

namespace Cosmos.DataTransfer.MongoExtension.Settings;
public class MongoSourceSettings : MongoBaseSettings
{
    public string? Collection { get; set; }

    [SensitiveValue]
    public Dictionary<string, IReadOnlyDictionary<string, object>>? KMSProviders { get; set; }

    public string? KeyVaultNamespace { get; set; }

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(ConnectionString))
        {
            yield return new ValidationResult($"{nameof(ConnectionString)} is required");
        }

        if (string.IsNullOrEmpty(DatabaseName))
        {
            yield return new ValidationResult($"{nameof(DatabaseName)} is required");
        }
    }
}
