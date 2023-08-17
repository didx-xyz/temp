﻿using FluentValidation;
using Microsoft.Extensions.Options;
using System.Transactions;
using Yoma.Core.Domain.Core.Extensions;
using Yoma.Core.Domain.Core.Interfaces;
using Yoma.Core.Domain.Core.Models;
using Yoma.Core.Domain.Entity.Interfaces;
using Yoma.Core.Domain.Lookups.Interfaces;
using Yoma.Core.Domain.Opportunity.Helpers;
using Yoma.Core.Domain.Opportunity.Interfaces;
using Yoma.Core.Domain.Opportunity.Interfaces.Lookups;
using Yoma.Core.Domain.Opportunity.Models;
using Yoma.Core.Domain.Opportunity.Services.Lookups;
using Yoma.Core.Domain.Opportunity.Validators;

namespace Yoma.Core.Domain.Opportunity.Services
{
    public class OpportunityService : IOpportunityService
    {
        #region Class Variables
        private readonly ScheduleJobOptions _scheduleJobOptions;

        private readonly IOpportunityStatusService _opportunityStatusService;
        private readonly IOpportunityCategoryService _opportunityCategoryService;
        private readonly ICountryService _countryService;
        private readonly IOrganizationService _organizationService;
        private readonly IOpportunityTypeService _opportunityTypeService;
        private readonly ILanguageService _languageService;
        private readonly ISkillService _skillService;
        private readonly IOpportunityDifficultyService _opportunityDifficultyService;
        private readonly ITimeIntervalService _timeIntervalService;

        private readonly OpportunityRequestValidator _opportunityRequestValidator;
        private readonly OpportunitySearchFilterInfoValidator _searchFilterInfoValidator;
        private readonly OpportunitySearchFilterValidator _searchFilterValidator;

        private readonly IRepositoryValueContainsWithNavigation<Models.Opportunity> _opportunityRepository;
        private readonly IRepository<OpportunityCategory> _opportunityCategoryRepository;
        private readonly IRepository<OpportunityCountry> _opportunityCountryRepository;
        private readonly IRepository<OpportunityLanguage> _opportunityLanguageRepository;
        private readonly IRepository<OpportunitySkill> _opportunitySkillRepository;

        public const string Separator_Keywords = " ";
        public static readonly Status[] Statuses_Updatable = { Status.Active, Status.Inactive };
        public static readonly Status[] Statuses_Activatable = { Status.Inactive };
        public static readonly Status[] Statuses_Deletable = { Status.Active, Status.Inactive };
        public static readonly Status[] Statuses_DeActivatable = { Status.Active };
        public static readonly Status[] Statuses_Expirable = { Status.Active, Status.Inactive };
        #endregion

        #region Constructor
        public OpportunityService(IOptions<ScheduleJobOptions> scheduleJobOptions,
            IOpportunityStatusService opportunityStatusService,
            IOpportunityCategoryService opportunityCategoryService,
            ICountryService countryService,
            IOrganizationService organizationService,
            IOpportunityTypeService opportunityTypeService,
            ILanguageService languageService,
            ISkillService skillService,
            IOpportunityDifficultyService opportunityDifficultyService,
            ITimeIntervalService timeIntervalService,
            OpportunityRequestValidator opportunityRequestValidator,
            OpportunitySearchFilterInfoValidator searchFilterInfoValidator,
            OpportunitySearchFilterValidator searchFilterValidator,
            IRepositoryValueContainsWithNavigation<Models.Opportunity> opportunityRepository,
            IRepository<OpportunityCategory> opportunityCategoryRepository,
            IRepository<OpportunityCountry> opportunityCountryRepository,
            IRepository<OpportunityLanguage> opportunityLanguageRepository,
            IRepository<OpportunitySkill> opportunitySkillRepository)
        {
            _scheduleJobOptions = scheduleJobOptions.Value;

            _opportunityStatusService = opportunityStatusService;
            _opportunityCategoryService = opportunityCategoryService;
            _countryService = countryService;
            _organizationService = organizationService;
            _opportunityTypeService = opportunityTypeService;
            _languageService = languageService;
            _skillService = skillService;
            _opportunityDifficultyService = opportunityDifficultyService;
            _timeIntervalService = timeIntervalService;

            _opportunityRequestValidator = opportunityRequestValidator;
            _searchFilterInfoValidator = searchFilterInfoValidator;
            _searchFilterValidator = searchFilterValidator;

            _opportunityRepository = opportunityRepository;
            _opportunityCategoryRepository = opportunityCategoryRepository;
            _opportunityCountryRepository = opportunityCountryRepository;
            _opportunityLanguageRepository = opportunityLanguageRepository;
            _opportunitySkillRepository = opportunitySkillRepository;
        }
        #endregion

        #region Public Members
        public Models.Opportunity GetById(Guid id, bool includeChildren)
        {
            if (id == Guid.Empty)
                throw new ArgumentNullException(nameof(id));

            var result = _opportunityRepository.Query(includeChildren).SingleOrDefault(o => o.Id == id)
                ?? throw new ArgumentOutOfRangeException(nameof(id), $"{nameof(Models.Opportunity)} with id '{id}' does not exist");

            return result;
        }

        public OpportunityInfo GetInfoById(Guid id, bool includeChildren)
        {
            var result = GetById(id, includeChildren);

            return result.ToOpportunityInfo();
        }

        public Models.Opportunity? GetByTitleOrNull(string title, bool includeChildItems)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentNullException(nameof(title));
            title = title.Trim();

            var result = _opportunityRepository.Query(includeChildItems).SingleOrDefault(o => o.Title == title);
            if (result == null) return null;

            return result;
        }

        public OpportunityInfo? GetInfoByTitleOrNull(string title, bool includeChildItems)
        {
            var result = GetByTitleOrNull(title, includeChildItems);
            if (result == null) return null;

            return result.ToOpportunityInfo();
        }

        public OpportunitySearchResultsInfo SearchInfo(OpportunitySearchFilterInfo filter)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            _searchFilterInfoValidator.ValidateAndThrow(filter);

            var filterInternal = new OpportunitySearchFilter
            {
                // active only
                StatusIds = new List<Guid> { _opportunityStatusService.GetByName(Status.Active.ToString()).Id },
                TypeIds = filter.TypeIds,
                CategoryIds = filter.CategoryIds,
                LanguageIds = filter.LanguageIds,
                CountryIds = filter.CountryIds,
                ValueContains = filter.ValueContains
            };

            var searchResult = Search(filterInternal);
            var results = new OpportunitySearchResultsInfo
            {
                TotalCount = searchResult.TotalCount,
                Items = searchResult.Items.Select(o => o.ToOpportunityInfo()).ToList()
            };

            return results;
        }

        public OpportunitySearchResults Search(OpportunitySearchFilter filter)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            _searchFilterValidator.ValidateAndThrow(filter);

            var query = _opportunityRepository.Query(true);

            //date range
            if (filter.StartDate.HasValue)
            {
                filter.StartDate = filter.StartDate.Value.RemoveTime();
                query = query.Where(o => o.DateCreated >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                filter.EndDate = filter.EndDate.Value.ToEndOfDay();
                query = query.Where(o => o.DateCreated <= filter.EndDate.Value);
            }

            //organization (explicitly specified)
            var filterByOrganization = false;
            var organizationIds = new List<Guid>();
            if (filter.OrganizationId.HasValue)
            {
                filterByOrganization = true;
                organizationIds.Add(filter.OrganizationId.Value);
            }

            //types (explicitly specified)
            var filterByType = false;
            var typeIds = new List<Guid>();
            if (filter.TypeIds != null)
            {
                filter.TypeIds = filter.TypeIds.Distinct().ToList();
                if (filter.TypeIds.Any())
                {
                    filterByType = true;
                    typeIds.AddRange(filter.TypeIds);
                }
            }

            //categories (explicitly specified)
            var filterByCategories = false;
            var categoryIds = new List<Guid>();
            if (filter.CategoryIds != null)
            {
                filter.CategoryIds = filter.CategoryIds.Distinct().ToList();
                if (filter.CategoryIds.Any())
                {
                    filterByCategories = true;
                    categoryIds.AddRange(filter.CategoryIds);
                }
            }

            //languages
            if (filter.LanguageIds != null)
            {
                filter.LanguageIds = filter.LanguageIds.Distinct().ToList();
                if (filter.LanguageIds.Any())
                {
                    var matchedOpportunityIds = _opportunityLanguageRepository.Query().Where(o => filter.LanguageIds.Contains(o.LanguageId)).Select(o => o.OpportunityId).ToList();
                    query = query.Where(o => matchedOpportunityIds.Contains(o.Id));
                }
            }

            //countries
            if (filter.CountryIds != null)
            {
                filter.CountryIds = filter.CountryIds.Distinct().ToList();
                if (filter.CountryIds.Any())
                {
                    var matchedOpportunityIds = _opportunityCountryRepository.Query().Where(o => filter.CountryIds.Contains(o.CountryId)).Select(o => o.OpportunityId).ToList();
                    query = query.Where(o => matchedOpportunityIds.Contains(o.Id));
                }
            }

            //statuses
            if (filter.StatusIds != null)
            {
                filter.StatusIds = filter.StatusIds.Distinct().ToList();
                if (filter.StatusIds.Any())
                    query = query.Where(o => filter.StatusIds.Contains(o.StatusId));
            }

            //valueContains (includes organizations, types, categories, opportunities and skills)
            if (!string.IsNullOrEmpty(filter.ValueContains))
            {
                var predicate = PredicateBuilder.False<Models.Opportunity>();

                //organizations
                var matchedOrganizationIds = _organizationService.Contains(filter.ValueContains).Select(o => o.Id).ToList();
                organizationIds.AddRange(matchedOrganizationIds.Except(organizationIds));
                predicate = predicate.Or(o => organizationIds.Contains(o.OrganizationId));

                //types
                var matchedTypeIds = _opportunityTypeService.Contains(filter.ValueContains).Select(o => o.Id).ToList();
                typeIds.AddRange(matchedTypeIds.Except(typeIds));
                predicate = predicate.Or(o => matchedTypeIds.Contains(o.TypeId));

                //categories
                var matchedCategoryIds = _opportunityCategoryService.Contains(filter.ValueContains).Select(o => o.Id).ToList();
                categoryIds.AddRange(matchedCategoryIds.Except(categoryIds));
                var matchedOpportunities = _opportunityCategoryRepository.Query().Where(o => categoryIds.Contains(o.CategoryId)).Select(o => o.OpportunityId).ToList();
                predicate = predicate.Or(o => matchedOpportunities.Contains(o.Id));

                //opportunities
                predicate = _opportunityRepository.Contains(predicate, filter.ValueContains);

                //skills
                var matchedSkillIds = _skillService.Contains(filter.ValueContains).Select(o => o.Id).ToList();
                matchedOpportunities = _opportunitySkillRepository.Query().Where(o => matchedSkillIds.Contains(o.SkillId)).Select(o => o.OpportunityId).ToList();
                predicate = predicate.Or(o => matchedOpportunities.Contains(o.Id));

                query = query.Where(predicate);
            }
            else
            {
                if (filterByOrganization)
                    query = query.Where(o => organizationIds.Contains(o.OrganizationId));

                if (filterByType)
                    query = query.Where(o => typeIds.Contains(o.TypeId));

                if (filterByCategories)
                {
                    var matchedOpportunities = _opportunityCategoryRepository.Query().Where(o => categoryIds.Contains(o.CategoryId)).Select(o => o.OpportunityId).ToList();
                    query = query.Where(o => matchedOpportunities.Contains(o.Id));
                }
            }

            var results = new OpportunitySearchResults();
            query = query.OrderByDescending(o => o.DateCreated);

            //pagination
            if (filter.PaginationEnabled)
            {
                results.TotalCount = query.Count();
                query = query.Skip((filter.PageNumber.Value - 1) * filter.PageSize.Value).Take(filter.PageSize.Value);
            }

            results.Items = query.ToList();

            return results;
        }

        public async Task<Models.Opportunity> Upsert(OpportunityRequest request, string? username)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            username = username?.Trim();
            if (string.IsNullOrEmpty(username))
                throw new ArgumentNullException(nameof(username));

            _opportunityRequestValidator.ValidateAndThrow(request);

            // check if user exists
            var isNew = !request.Id.HasValue;

            var result = !request.Id.HasValue ? new Models.Opportunity { Id = Guid.NewGuid() } : GetById(request.Id.Value, true);

            var existingByTitle = GetByTitleOrNull(request.Title, false);
            if (existingByTitle != null && (isNew || result.Id != existingByTitle.Id))
                throw new ValidationException($"{nameof(Models.Opportunity)} with the specified name '{request.Title}' already exists");

            result.Title = request.Title;
            result.Description = request.Description;
            result.TypeId = request.TypeId;
            result.Type = _opportunityTypeService.GetById(request.TypeId).Name;
            result.OrganizationId = request.OrganizationId;
            result.Organization = _organizationService.GetById(request.OrganizationId, false).Name;
            result.Instructions = request.Instructions;
            result.URL = request.URL;
            result.ZltoReward = request.ZltoReward;
            result.YomaReward = request.YomaReward;
            result.ZltoRewardPool = request.ZltoRewardPool;
            result.YomaRewardPool = request.YomaRewardPool;
            result.VerificationSupported = request.VerificationSupported;
            result.DifficultyId = request.DifficultyId;
            result.Difficulty = _opportunityDifficultyService.GetById(request.DifficultyId).Name;
            result.CommitmentIntervalId = request.CommitmentIntervalId;
            result.CommitmentInterval = _timeIntervalService.GetById(request.CommitmentIntervalId).Name;
            result.CommitmentIntervalCount = request.CommitmentIntervalCount;
            result.ParticipantLimit = request.ParticipantLimit;
            result.Keywords = request.Keywords == null ? null : string.Join(Separator_Keywords, request.Keywords);
            result.DateStart = request.DateStart.RemoveTime();
            result.DateEnd = !request.DateEnd.HasValue ? null : request.DateEnd.Value.ToEndOfDay();

            if (isNew)
            {
                var status = request.PostAsActive ? Status.Active : Status.Inactive;
                result.StatusId = _opportunityStatusService.GetByName(status.ToString()).Id;
                result.Status = status;
                result.CreatedBy = username;
                result = await _opportunityRepository.Create(result);
            }
            else
            {
                if (!Statuses_Updatable.Contains(result.Status))
                    throw new InvalidOperationException($"{nameof(Models.Opportunity)} can no longer be updated (current status '{result.Status}')");

                result.ModifiedBy = username;
                await _opportunityRepository.Update(result);
                result.DateModified = DateTimeOffset.Now;
            }

            return result;
        }

        public async Task IncrementParticipantCount(Guid id, int increment = 1)
        {
            if (increment <= 0)
                throw new ArgumentOutOfRangeException(nameof(increment));

            var opportunity = GetById(id, false);
            if (opportunity.Status != Status.Active)
                throw new InvalidOperationException($"{nameof(Models.Opportunity)} must be active (current status '{opportunity.Status}')");

            if (opportunity.DateStart > DateTimeOffset.Now)
                throw new InvalidOperationException($"{nameof(Models.Opportunity)} is active but has not started (start date '{opportunity.DateStart}')");

            var count = opportunity.ParticipantCount += increment;
            if (opportunity.ParticipantLimit.HasValue && count > opportunity.ParticipantLimit)
                throw new InvalidOperationException($"Increment will exceed limit (current count '{opportunity.ParticipantCount ?? 0}' vs current limit '{opportunity.ParticipantLimit}')");

            opportunity.ParticipantCount = count;
            //modified by system; ModifiedBy not set

            await _opportunityRepository.Update(opportunity);
        }

        public async Task UpdateStatus(Guid id, Status status, string? username)
        {
            var opportunity = GetById(id, false);

            username = username?.Trim();
            if (string.IsNullOrEmpty(username))
                throw new ArgumentNullException(nameof(username));

            switch (status)
            {
                case Status.Active:
                    if (opportunity.Status == Status.Active) return;
                    if (!Statuses_Activatable.Contains(opportunity.Status))
                        throw new InvalidOperationException($"{nameof(Models.Opportunity)} can not be activated (current status '{opportunity.Status}')");
                    break;

                case Status.Inactive:
                    if (opportunity.Status == Status.Inactive) return;
                    if (!Statuses_DeActivatable.Contains(opportunity.Status))
                        throw new InvalidOperationException($"{nameof(Models.Opportunity)} can not be deactivated (current status '{opportunity.Status}')");
                    break;

                case Status.Deleted:
                    if (opportunity.Status == Status.Deleted) return;
                    if (!Statuses_Deletable.Contains(opportunity.Status))
                        throw new InvalidOperationException($"{nameof(Models.Opportunity)} can not be deleted (current status '{opportunity.Status}')");

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(status), $"{nameof(Status)} of '{status}' not supported. Only statuses '{Status.Inactive} and {Status.Deleted} can be explicitly set");
            }

            var statusId = _opportunityStatusService.GetByName(status.ToString()).Id;

            opportunity.StatusId = statusId;
            opportunity.Status = status;
            opportunity.ModifiedBy = username;

            await _opportunityRepository.Update(opportunity);
        }

        public async Task ProcessExpiration()
        {
            var statusExpiredId = _opportunityStatusService.GetByName(Status.Expired.ToString()).Id;
            var statusExpirableIds = Statuses_Expirable.Select(o => _opportunityStatusService.GetByName(o.ToString()).Id).ToList();

            do
            {
                var items = _opportunityRepository.Query().Where(o => statusExpirableIds.Contains(o.StatusId) &&
                    o.DateEnd.HasValue && o.DateEnd.Value <= DateTimeOffset.Now).OrderBy(o => o.DateEnd).Take(_scheduleJobOptions.OpportunityExpirationBatchSize).ToList();
                if (!items.Any()) break;

                foreach (var item in items)
                {
                    //TODO: email notification to provider

                    item.StatusId = statusExpiredId;
                    await _opportunityRepository.Update(item);
                }
            } while (true);
        }

        public async Task ExpirationNotifications()
        {
            var datetimeFrom = new DateTimeOffset(DateTime.Today);
            var datetimeTo = datetimeFrom.AddDays(_scheduleJobOptions.OpportunityExpirationNotificationIntervalInDays);
            var statusExpirableIds = Statuses_Expirable.Select(o => _opportunityStatusService.GetByName(o.ToString()).Id).ToList();

            do
            {
                var items = _opportunityRepository.Query().Where(o => statusExpirableIds.Contains(o.StatusId) &&
                    o.DateEnd.HasValue && o.DateEnd.Value >= datetimeFrom && o.DateEnd.Value <= datetimeTo).OrderBy(o => o.DateEnd).Take(_scheduleJobOptions.OpportunityExpirationBatchSize).ToList();
                if (!items.Any()) break;

                //TODO: email notification to provider

            } while (true);
        }

        public async Task AssignCategories(Guid id, List<Guid> categoryIds)
        {
            var opportunity = GetById(id, false);

            if (categoryIds == null || !categoryIds.Any())
                throw new ArgumentNullException(nameof(categoryIds));

            if (!Statuses_Updatable.Contains(opportunity.Status))
                throw new InvalidOperationException($"{nameof(Models.Opportunity)} can no longer be updated (current status '{opportunity.Status}')");

            using var scope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled);
            foreach (var categoryId in categoryIds)
            {
                var category = _opportunityCategoryService.GetById(categoryId);

                var item = _opportunityCategoryRepository.Query().SingleOrDefault(o => o.OpportunityId == opportunity.Id && o.CategoryId == category.Id);
                if (item != null) return;

                item = new OpportunityCategory
                {
                    OpportunityId = opportunity.Id,
                    CategoryId = category.Id
                };

                await _opportunityCategoryRepository.Create(item);
            }

            scope.Complete();
        }

        public async Task DeleteCategories(Guid id, List<Guid> categoryIds)
        {
            var opportunity = GetById(id, false);

            if (categoryIds == null || !categoryIds.Any())
                throw new ArgumentNullException(nameof(categoryIds));

            if (!Statuses_Updatable.Contains(opportunity.Status))
                throw new InvalidOperationException($"{nameof(Models.Opportunity)} can no longer be updated (current status '{opportunity.Status}')");

            using var scope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled);
            foreach (var categoryId in categoryIds)
            {
                var category = _opportunityCategoryService.GetById(categoryId);

                var item = _opportunityCategoryRepository.Query().SingleOrDefault(o => o.OpportunityId == opportunity.Id && o.CategoryId == category.Id);
                if (item == null) return;

                await _opportunityCategoryRepository.Delete(item);
            }
        }

        public async Task AssignCountries(Guid id, List<Guid> countryIds)
        {
            var opportunity = GetById(id, false);

            if (countryIds == null || !countryIds.Any())
                throw new ArgumentNullException(nameof(countryIds));

            if (!Statuses_Updatable.Contains(opportunity.Status))
                throw new InvalidOperationException($"{nameof(Models.Opportunity)} can no longer be updated (current status '{opportunity.Status}')");

            using var scope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled);
            foreach (var countryId in countryIds)
            {
                var country = _countryService.GetById(countryId);

                var item = _opportunityCountryRepository.Query().SingleOrDefault(o => o.OpportunityId == opportunity.Id && o.CountryId == country.Id);
                if (item != null) return;

                item = new OpportunityCountry
                {
                    OpportunityId = opportunity.Id,
                    CountryId = country.Id
                };

                await _opportunityCountryRepository.Create(item);
            }

            scope.Complete();
        }

        public async Task DeleteCountries(Guid id, List<Guid> countryIds)
        {
            var opportunity = GetById(id, false);

            if (countryIds == null || !countryIds.Any())
                throw new ArgumentNullException(nameof(countryIds));

            if (!Statuses_Updatable.Contains(opportunity.Status))
                throw new InvalidOperationException($"{nameof(Models.Opportunity)} can no longer be updated (current status '{opportunity.Status}')");

            using var scope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled);
            foreach (var countryId in countryIds)
            {
                var country = _countryService.GetById(countryId);

                var item = _opportunityCountryRepository.Query().SingleOrDefault(o => o.OpportunityId == opportunity.Id && o.CountryId == country.Id);
                if (item == null) return;

                await _opportunityCountryRepository.Delete(item);
            }
        }

        public async Task AssignLanguages(Guid id, List<Guid> languageIds)
        {
            var opportunity = GetById(id, false);

            if (languageIds == null || !languageIds.Any())
                throw new ArgumentNullException(nameof(languageIds));

            if (!Statuses_Updatable.Contains(opportunity.Status))
                throw new InvalidOperationException($"{nameof(Models.Opportunity)} can no longer be updated (current status '{opportunity.Status}')");

            using var scope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled);
            foreach (var languageId in languageIds)
            {
                var language = _languageService.GetById(languageId);

                var item = _opportunityLanguageRepository.Query().SingleOrDefault(o => o.OpportunityId == opportunity.Id && o.LanguageId == language.Id);
                if (item != null) return;

                item = new OpportunityLanguage
                {
                    OpportunityId = opportunity.Id,
                    LanguageId = language.Id
                };

                await _opportunityLanguageRepository.Create(item);
            }

            scope.Complete();
        }

        public async Task DeleteLanguages(Guid id, List<Guid> languageIds)
        {
            var opportunity = GetById(id, false);

            if (languageIds == null || !languageIds.Any())
                throw new ArgumentNullException(nameof(languageIds));

            if (!Statuses_Updatable.Contains(opportunity.Status))
                throw new InvalidOperationException($"{nameof(Models.Opportunity)} can no longer be updated (current status '{opportunity.Status}')");

            using var scope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled);
            foreach (var languageId in languageIds)
            {
                var language = _languageService.GetById(languageId);

                var item = _opportunityLanguageRepository.Query().SingleOrDefault(o => o.OpportunityId == opportunity.Id && o.LanguageId == language.Id);
                if (item == null) return;

                await _opportunityLanguageRepository.Delete(item);
            }
        }

        public async Task AssignSkills(Guid id, List<Guid> skillIds)
        {
            var opportunity = GetById(id, false);

            if (skillIds == null || !skillIds.Any())
                throw new ArgumentNullException(nameof(skillIds));

            if (!Statuses_Updatable.Contains(opportunity.Status))
                throw new InvalidOperationException($"{nameof(Models.Opportunity)} can no longer be updated (current status '{opportunity.Status}')");

            using var scope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled);
            foreach (var skillId in skillIds)
            {
                var skill = _skillService.GetById(skillId);

                var item = _opportunitySkillRepository.Query().SingleOrDefault(o => o.OpportunityId == opportunity.Id && o.SkillId == skill.Id);
                if (item != null) return;

                item = new OpportunitySkill
                {
                    OpportunityId = opportunity.Id,
                    SkillId = skill.Id
                };

                await _opportunitySkillRepository.Create(item);
            }

            scope.Complete();
        }

        public async Task DeleteSkills(Guid id, List<Guid> skillIds)
        {
            var opportunity = GetById(id, false);

            if (skillIds == null || !skillIds.Any())
                throw new ArgumentNullException(nameof(skillIds));

            if (!Statuses_Updatable.Contains(opportunity.Status))
                throw new InvalidOperationException($"{nameof(Models.Opportunity)} can no longer be updated (current status '{opportunity.Status}')");

            using var scope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled);
            foreach (var skillId in skillIds)
            {
                var skill = _skillService.GetById(skillId);

                var item = _opportunitySkillRepository.Query().SingleOrDefault(o => o.OpportunityId == opportunity.Id && o.SkillId == skill.Id);
                if (item == null) return;

                await _opportunitySkillRepository.Delete(item);
            }
        }
        #endregion
    }
}
