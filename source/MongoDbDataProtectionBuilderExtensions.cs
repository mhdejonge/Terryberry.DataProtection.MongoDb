﻿namespace Terryberry.DataProtection.MongoDb
{
    using System;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.DataProtection.KeyManagement;
    using Microsoft.Extensions.DependencyInjection;
    using MongoDB.Driver;

    /// <summary>
    /// Contains MongoDb-specific extension methods for modifying a <see cref="IDataProtectionBuilder" />.
    /// </summary>
    public static class MongoDbDataProtectionBuilderExtensions
    {
        /// <summary>
        /// Default id name.
        /// </summary>
        private const string Id = "id";

        /// <summary>
        /// Configures the data protection system to persist keys to the specified database and collection in MongoDb.
        /// </summary>
        /// <param name="builder">The builder instance to modify.</param>
        /// <param name="connectionString">MongoDb connection url.</param>
        /// <param name="databaseName">Database used to store the key list.</param>
        /// <param name="collectionName">Collection used to store the key list.</param>
        /// <param name="idName">The name of the id attribute on the keys being inserted. Must be on the top level element.</param>
        /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
        public static IDataProtectionBuilder PersistKeysToMongoDb(this IDataProtectionBuilder builder, string connectionString, string databaseName, string collectionName, string idName = Id)
        {
            return builder.PersistKeysToMongoDb(new MongoClient(connectionString).GetDatabase(databaseName), collectionName, idName);
        }

        /// <summary>
        /// Configures the data protection system to persist keys to the specified database and collection in MongoDb.
        /// </summary>
        /// <param name="builder">The builder instance to modify.</param>
        /// <param name="database">Database used to store the key list.</param>
        /// <param name="collectionName">Collection used to store the key list.</param>
        /// <param name="idName">The name of the id attribute on the keys being inserted. Must be on the top level element.</param>
        /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
        public static IDataProtectionBuilder PersistKeysToMongoDb(this IDataProtectionBuilder builder, IMongoDatabase database, string collectionName, string idName = Id)
        {
            if (database is null) throw new ArgumentNullException(nameof(database));
            return builder.PersistKeysToMongoDb(database.GetCollection<MongoDbXmlKey>(collectionName), idName);
        }

        /// <summary>
        /// Configures the data protection system to persist keys to the specified database and collection in MongoDb.
        /// </summary>
        /// <param name="builder">The builder instance to modify.</param>
        /// <param name="collection">Collection used to store the key list.</param>
        /// <param name="idName">The name of the id attribute on the keys being inserted. Must be on the top level element.</param>
        /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
        public static IDataProtectionBuilder PersistKeysToMongoDb(this IDataProtectionBuilder builder, IMongoCollection<MongoDbXmlKey> collection, string idName = Id)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (collection is null) throw new ArgumentNullException(nameof(collection));
            if (string.IsNullOrWhiteSpace(idName)) throw new ArgumentException($"{nameof(idName)} cannot be null or white space.", nameof(idName));
            builder.Services.Configure<KeyManagementOptions>(options =>
            {
                var mongoDbXmlRepository = new MongoDbXmlRepository(collection, idName.Trim());
                options.XmlRepository = mongoDbXmlRepository;
            });
            return builder;
        }

        /// <summary>
        /// Removes keys from the MongoDb repository after they expire or are revoked. If a custom key manager is used, it must be configured before calling this method.
        /// </summary>
        /// <param name="builder">The builder instance to modify.</param>
        /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
        /// <exception cref="InvalidOperationException">PersistKeysToMongoDb must be called before this method.</exception>
        /// <remarks>
        /// Cleanup will run after this method is called, and whenever the key manager calls <see cref="Microsoft.AspNetCore.DataProtection.Repositories.IXmlRepository.StoreElement" />.
        /// </remarks>
        public static IDataProtectionBuilder AddKeyCleanup(this IDataProtectionBuilder builder)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            var keyManager = builder.Services.BuildServiceProvider().GetService<IKeyManager>();
            builder.Services.Configure<KeyManagementOptions>(options =>
            {
                if (options.XmlRepository is MongoDbXmlRepository mongodbXmlRepository) mongodbXmlRepository.SetKeyManager(keyManager);
                else throw new InvalidOperationException($"{nameof(PersistKeysToMongoDb)} must be called before {nameof(AddKeyCleanup)}.");
            });
            return builder;
        }
    }
}