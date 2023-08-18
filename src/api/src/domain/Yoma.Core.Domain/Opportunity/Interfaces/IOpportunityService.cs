﻿using Yoma.Core.Domain.Opportunity.Models;

namespace Yoma.Core.Domain.Opportunity.Interfaces
{
    public interface IOpportunityService
    {
        Models.Opportunity GetById(Guid id, bool includeChildren, bool ensureOrganizationAuthorization);

        OpportunityInfo GetInfoById(Guid id, bool includeChildren);

        Models.Opportunity? GetByTitleOrNull(string title, bool includeChildItems);

        OpportunityInfo? GetInfoByTitleOrNull(string title, bool includeChildItems);

        OpportunitySearchResultsInfo SearchInfo(OpportunitySearchFilterInfo filter);

        OpportunitySearchResults Search(OpportunitySearchFilter filter, bool ensureOrganizationAuthorization);

        Task<Models.Opportunity> Upsert(OpportunityRequest request, bool ensureOrganizationAuthorization);

        Task IncrementParticipantCount(Guid id, int increment = 1);

        Task UpdateStatus(Guid id, Status status, bool ensureOrganizationAuthorization);

        Task AssignCategories(Guid id, List<Guid> categoryIds, bool ensureOrganizationAuthorization);

        Task DeleteCategories(Guid id, List<Guid> categoryIds, bool ensureOrganizationAuthorization);

        Task AssignCountries(Guid id, List<Guid> countryIds, bool ensureOrganizationAuthorization);

        Task DeleteCountries(Guid id, List<Guid> countryIds, bool ensureOrganizationAuthorization);

        Task AssignLanguages(Guid id, List<Guid> languageIds, bool ensureOrganizationAuthorization);

        Task DeleteLanguages(Guid id, List<Guid> languageIds, bool ensureOrganizationAuthorization);

        Task AssignSkills(Guid id, List<Guid> skillIds, bool ensureOrganizationAuthorization);

        Task DeleteSkills(Guid id, List<Guid> skillIds, bool ensureOrganizationAuthorization);
    }
}
