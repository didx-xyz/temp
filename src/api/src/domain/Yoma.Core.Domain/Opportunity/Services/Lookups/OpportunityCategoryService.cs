﻿using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Yoma.Core.Domain.Core.Interfaces;
using Yoma.Core.Domain.Core.Models;
using Yoma.Core.Domain.Opportunity.Interfaces.Lookups;
using Yoma.Core.Domain.Opportunity.Models.Lookups;

namespace Yoma.Core.Domain.Opportunity.Services.Lookups
{
    public class OpportunityCategoryService : IOpportunityCategoryService
    {
        #region Class Variables
        private readonly AppSettings _appSettings;
        private readonly IMemoryCache _memoryCache;
        private readonly IRepository<OpportunityCategory> _opportunityCategoryRepository;
        #endregion

        #region Constructor
        public OpportunityCategoryService(IOptions<AppSettings> appSettings,
            IMemoryCache memoryCache,
            IRepository<OpportunityCategory> opportunityCategoryRepository)
        {
            _appSettings = appSettings.Value;
            _memoryCache = memoryCache;
            _opportunityCategoryRepository = opportunityCategoryRepository;
        }
        #endregion

        #region Public Members
        public OpportunityCategory GetByName(string name)
        {
            var result = GetByNameOrNull(name);

            if (result == null)
                throw new ArgumentException($"{nameof(OpportunityCategory)} with name '{name}' does not exists", nameof(name));

            return result;
        }

        public OpportunityCategory? GetByNameOrNull(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));
            name = name.Trim();

            return List().SingleOrDefault(o => o.Name == name);
        }

        public OpportunityCategory GetById(Guid id)
        {
            var result = GetByIdOrNull(id);

            if (result == null)
                throw new ArgumentException($"{nameof(OpportunityCategory)} for '{id}' does not exists", nameof(id));

            return result;
        }

        public OpportunityCategory? GetByIdOrNull(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentNullException(nameof(id));

            return List().SingleOrDefault(o => o.Id == id);
        }

        public List<OpportunityCategory> List()
        {
            if (!_appSettings.CacheEnabledByReferenceDataTypes.HasFlag(Core.ReferenceDataType.Lookups))
                return _opportunityCategoryRepository.Query().ToList();

            var result = _memoryCache.GetOrCreate(nameof(OpportunityCategory), entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(_appSettings.CacheSlidingExpirationLookupInHours);
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(_appSettings.CacheAbsoluteExpirationRelativeToNowLookupInDays);
                return _opportunityCategoryRepository.Query().OrderBy(o => o.Name).ToList();
            });

            if (result == null)
                throw new InvalidOperationException($"Failed to retrieve cached list of '{nameof(OpportunityCategory)}s'");

            return result;
        }
        #endregion
    }
}
