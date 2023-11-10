﻿using Microsoft.EntityFrameworkCore;
using Stac;
using Stac.Api.Interfaces;
using Stac.Api.WebApi.Implementations.Default;

namespace GeoCop.Api.StacServices
{
    /// <summary>
    /// Provides access to STAC collections.
    /// </summary>
    public class StacCollectionsProvider : ICollectionsProvider
    {
        private readonly ILogger<StacCollectionsProvider> logger;
        private readonly IDbContextFactory<Context> contextFactory;
        private readonly StacConverter stacConverter;

        /// <summary>
        /// Initializes a new instance of the <see cref="StacCollectionsProvider"/> class.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="contextFactory"></param>
        /// <param name="stacConverter"></param>
        public StacCollectionsProvider(ILogger<StacCollectionsProvider> logger, IDbContextFactory<Context> contextFactory, StacConverter stacConverter)
        {
            this.logger = logger;
            this.contextFactory = contextFactory;
            this.stacConverter = stacConverter;
        }

        /// <inheritdoc/>
        public Task<StacCollection> GetCollectionByIdAsync(string collectionId, IStacApiContext stacApiContext, CancellationToken cancellationToken = default)
        {
            try
            {
                using var db = contextFactory.CreateDbContext();
                var deliveryMandate = db.DeliveryMandatesWithIncludes.First(dm => (StacConverter.CollectionIdPrefix + dm.Id) == collectionId);
                var collection = stacConverter.ToStacCollection(deliveryMandate);
                return Task.FromResult(collection);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error while getting collection with id {collectionId}. Collection might not exist.");
                return Task.FromResult<StacCollection>(null);
            }
        }

        /// <inheritdoc/>
        public Task<IEnumerable<StacCollection>> GetCollectionsAsync(IStacApiContext stacApiContext, CancellationToken cancellationToken = default)
        {
            using var db = contextFactory.CreateDbContext();
            var collections = db.DeliveryMandatesWithIncludes.Select(stacConverter.ToStacCollection).ToList();
            stacApiContext.Properties.SetProperty(DefaultConventions.MatchedCountPropertiesKey, collections.Count);

            return Task.FromResult<IEnumerable<StacCollection>>(collections);
        }
    }
}
