using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using Cosmos.DataTransfer.Interfaces;
using Cosmos.DataTransfer.MongoExtension.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Cosmos.DataTransfer.MongoExtension;
[Export(typeof(IDataSourceExtension))]
internal class MongoDataSourceExtension : IDataSourceExtensionWithSettings
{
    public string DisplayName => "MongoDB";

    public async IAsyncEnumerable<IDataItem> ReadAsync(IConfiguration config, ILogger logger, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var settings = config.Get<MongoSourceSettings>();
        settings.Validate();

        if (settings != null && !string.IsNullOrEmpty(settings.ConnectionString) && !string.IsNullOrEmpty(settings.DatabaseName))
        {
            var context = new Context(settings.ConnectionString, settings.DatabaseName, settings.KeyVaultNamespace, settings.KMSProviders);

            var collectionNames = !string.IsNullOrEmpty(settings.Collection)
                ? new List<string> { settings.Collection }
                : await context.GetCollectionNamesAsync();

            foreach (var collection in collectionNames)
            {
                await foreach (var item in EnumerateCollectionAsync(context, collection, logger, settings.BatchSize).WithCancellation(cancellationToken))
                {
                    yield return item;
                }
            }
        }
    }

    public async IAsyncEnumerable<IDataItem> EnumerateCollectionAsync(Context context, string collectionName, ILogger logger, int batchSize)
    {
        logger.LogInformation("Reading collection '{Collection}'", collectionName);
        var collection = context.GetCollection<BsonDocument>(collectionName);
        int itemCount = 0;

        using (var cursor = await collection.FindAsync<BsonDocument>(new BsonDocument().BatchSize(batchSize)))
        {
            while (await cursor.MoveNextAsync())
            {
                foreach (var record in cursor.Current)
                {
                    yield return new MongoDataItem(record);
                    itemCount++;
                }
            }
        }

        if (itemCount > 0)
            logger.LogInformation("Read {ItemCount} items from collection '{Collection}'", itemCount, collectionName);
        else
            logger.LogWarning("No items read from collection '{Collection}'", collectionName);
    }

    public IEnumerable<IDataExtensionSettings> GetSettings()
    {
        yield return new MongoSourceSettings();
    }
}