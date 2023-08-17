﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using Yoma.Core.Domain.Core;
using Yoma.Core.Domain.Opportunity.Interfaces;
using Yoma.Core.Domain.Opportunity.Interfaces.Lookups;
using Yoma.Core.Domain.Opportunity.Models;
using Yoma.Core.Domain.Opportunity.Models.Lookups;

namespace Yoma.Core.Api.Controllers
{
    [Route("api/v3/opportunity")]
    [ApiController]
    [Authorize(Policy = Common.Constants.Authorization_Policy, Roles = $"{Constants.Role_Admin}, {Constants.Role_OrganizationAdmin}")]
    [SwaggerTag("(by default, Admin or Organization Admin roles required)")]
    public class OpportunityController : Controller
    {
        #region Class Variables
        private readonly ILogger<OpportunityController> _logger;
        private readonly IOpportunityService _opportunityService;
        private readonly IOpportunityCategoryService _opportunityCategoryService;
        private readonly IOpportunityDifficultyService _opportunityDifficultyService;
        private readonly IOpportunityStatusService _opportunityStatusService;
        private readonly IOpportunityTypeService _opportunityTypeService;
        #endregion

        #region Constructor
        public OpportunityController(
            ILogger<OpportunityController> logger,
            IOpportunityService opportunityService,
            IOpportunityCategoryService opportunityCategoryService,
            IOpportunityDifficultyService opportunityDifficultyService,
            IOpportunityStatusService opportunityStatusService,
            IOpportunityTypeService opportunityTypeService
            )
        {
            _logger = logger;
            _opportunityService = opportunityService;
            _opportunityCategoryService = opportunityCategoryService;
            _opportunityDifficultyService = opportunityDifficultyService;
            _opportunityStatusService = opportunityStatusService;
            _opportunityTypeService = opportunityTypeService;
        }
        #endregion

        #region Public Members
        #region Anonymous Actions
        [SwaggerOperation(Summary = "Get the specified opportunity by id (Anonymous)")]
        [HttpGet("{id}/info")]
        [ProducesResponseType(typeof(OpportunityInfo), (int)HttpStatusCode.OK)]
        [AllowAnonymous]
        public IActionResult GetInfoById([FromRoute] Guid id)
        {
            _logger.LogInformation("Handling request {requestName}", nameof(GetInfoById));

            var result = _opportunityService.GetInfoById(id, true);

            _logger.LogInformation("Request {requestName} handled", nameof(GetInfoById));

            return StatusCode((int)HttpStatusCode.OK, result);
        }

        [SwaggerOperation(Summary = "Search for opportunities based on the supplied filter (Anonymous)")]
        [HttpPost("info/search")]
        [ProducesResponseType(typeof(List<OpportunitySearchResultsInfo>), (int)HttpStatusCode.OK)]
        [AllowAnonymous]
        public IActionResult SearchInfo([FromBody] OpportunitySearchFilterInfo filter)
        {
            _logger.LogInformation("Handling request {requestName}", nameof(SearchInfo));

            var result = _opportunityService.SearchInfo(filter);

            _logger.LogInformation("Request {requestName} handled", nameof(SearchInfo));

            return StatusCode((int)HttpStatusCode.OK, result);
        }
        #endregion

        #region Administrative Actions
        [SwaggerOperation(Summary = "Return a list of opportunity categories")]
        [HttpGet("category")]
        [ProducesResponseType(typeof(List<Domain.Opportunity.Models.Lookups.OpportunityCategory>), (int)HttpStatusCode.OK)]
        public IActionResult ListOpportunityCategories()
        {
            _logger.LogInformation("Handling request {requestName}", nameof(ListOpportunityCategories));

            var result = _opportunityCategoryService.List();

            _logger.LogInformation("Request {requestName} handled", nameof(ListOpportunityCategories));

            return StatusCode((int)HttpStatusCode.OK, result);
        }

        [SwaggerOperation(Summary = "Return a list of opportunity difficulties")]
        [HttpGet("difficulty")]
        [ProducesResponseType(typeof(List<OpportunityDifficulty>), (int)HttpStatusCode.OK)]
        public IActionResult ListOpportunityDifficulties()
        {
            _logger.LogInformation("Handling request {requestName}", nameof(ListOpportunityDifficulties));

            var result = _opportunityDifficultyService.List();

            _logger.LogInformation("Request {requestName} handled", nameof(ListOpportunityDifficulties));

            return StatusCode((int)HttpStatusCode.OK, result);
        }

        [SwaggerOperation(Summary = "Return a list of opportunity statuses")]
        [HttpGet("status")]
        [ProducesResponseType(typeof(List<OpportunityStatus>), (int)HttpStatusCode.OK)]
        public IActionResult ListOpportunityStatuses()
        {
            _logger.LogInformation("Handling request {requestName}", nameof(ListOpportunityStatuses));

            var result = _opportunityStatusService.List();

            _logger.LogInformation("Request {requestName} handled", nameof(ListOpportunityStatuses));

            return StatusCode((int)HttpStatusCode.OK, result);
        }

        [SwaggerOperation(Summary = "Return a list of opportunity types")]
        [HttpGet("type")]
        [ProducesResponseType(typeof(List<OpportunityType>), (int)HttpStatusCode.OK)]
        public IActionResult ListOpportunityTypes()
        {
            _logger.LogInformation("Handling request {requestName}", nameof(ListOpportunityTypes));

            var result = _opportunityTypeService.List();

            _logger.LogInformation("Request {requestName} handled", nameof(ListOpportunityTypes));

            return StatusCode((int)HttpStatusCode.OK, result);
        }

        [SwaggerOperation(Summary = "Get the specified opportunity by id")]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Opportunity), (int)HttpStatusCode.OK)]
        public IActionResult GetById([FromRoute] Guid id)
        {
            _logger.LogInformation("Handling request {requestName}", nameof(GetById));

            var result = _opportunityService.GetById(id, true);

            _logger.LogInformation("Request {requestName} handled", nameof(GetById));

            return StatusCode((int)HttpStatusCode.OK, result);
        }

        [SwaggerOperation(Summary = "Search for opportunities based on the supplied filter")]
        [HttpPost("search")]
        [ProducesResponseType(typeof(List<OpportunitySearchResults>), (int)HttpStatusCode.OK)]
        [AllowAnonymous]
        public IActionResult Search([FromBody] OpportunitySearchFilter filter)
        {
            _logger.LogInformation("Handling request {requestName}", nameof(SearchInfo));

            var result = _opportunityService.Search(filter);

            _logger.LogInformation("Request {requestName} handled", nameof(SearchInfo));

            return StatusCode((int)HttpStatusCode.OK, result);
        }

        [SwaggerOperation(Summary = "Insert or update an opportunity")]
        [HttpPost()]
        [ProducesResponseType(typeof(Opportunity), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Upsert([FromBody] OpportunityRequest request)
        {
            _logger.LogInformation("Handling request {requestName} {paramName}: {paramValue}",
                nameof(Upsert), nameof(request), !request.Id.HasValue ? "insert" : $"update: {request.Id.Value}");

            var result = await _opportunityService.Upsert(request);

            _logger.LogInformation("Request {requestName} handled", nameof(Upsert));

            return StatusCode((int)HttpStatusCode.OK, result);
        }

        [SwaggerOperation(Summary = "Assign category(ies) to the specified opportunity")]
        [HttpPut("{id}/assign/categories")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> AssignCategories([FromRoute] Guid id, [FromBody] List<Guid> categoryIds)
        {
            _logger.LogInformation("Handling request {requestName}", nameof(AssignCategories));

            await _opportunityService.AssignCategories(id, categoryIds);

            _logger.LogInformation("Request {requestName} handled", nameof(AssignCategories));

            return StatusCode((int)HttpStatusCode.OK);
        }

        [SwaggerOperation(Summary = "Remove category(ies) from the specified opportunity")]
        [HttpDelete("{id}/remove/categories")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteCategories([FromRoute] Guid id, [FromBody] List<Guid> categoryIds)
        {
            _logger.LogInformation("Handling request {requestName}", nameof(DeleteCategories));

            await _opportunityService.DeleteCategories(id, categoryIds);

            _logger.LogInformation("Request {requestName} handled", nameof(DeleteCategories));

            return StatusCode((int)HttpStatusCode.OK);
        }

        [SwaggerOperation(Summary = "Assign country(ies) to the specified opportunity")]
        [HttpPut("{id}/assign/countries")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> AssignCountries([FromRoute] Guid id, [FromBody] List<Guid> countryIds)
        {
            _logger.LogInformation("Handling request {requestName}", nameof(AssignCountries));

            await _opportunityService.AssignCountries(id, countryIds);

            _logger.LogInformation("Request {requestName} handled", nameof(AssignCountries));

            return StatusCode((int)HttpStatusCode.OK);
        }

        [SwaggerOperation(Summary = "Remove country(ies) from the specified opportunity")]
        [HttpDelete("{id}/remove/countries")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteCountries([FromRoute] Guid id, [FromBody] List<Guid> countryIds)
        {
            _logger.LogInformation("Handling request {requestName}", nameof(DeleteCountries));

            await _opportunityService.DeleteCountries(id, countryIds);

            _logger.LogInformation("Request {requestName} handled", nameof(DeleteCountries));

            return StatusCode((int)HttpStatusCode.OK);
        }

        [SwaggerOperation(Summary = "Assign language(s) to the specified opportunity")]
        [HttpPut("{id}/assign/languages")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> AssignLanguages([FromRoute] Guid id, [FromBody] List<Guid> languageIds)
        {
            _logger.LogInformation("Handling request {requestName}", nameof(AssignLanguages));

            await _opportunityService.AssignLanguages(id, languageIds);

            _logger.LogInformation("Request {requestName} handled", nameof(AssignLanguages));

            return StatusCode((int)HttpStatusCode.OK);
        }

        [SwaggerOperation(Summary = "Remove language(s) from the specified opportunity")]
        [HttpDelete("{id}/remove/languages")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteLanguages([FromRoute] Guid id, [FromBody] List<Guid> languageIds)
        {
            _logger.LogInformation("Handling request {requestName}", nameof(DeleteLanguages));

            await _opportunityService.DeleteLanguages(id, languageIds);

            _logger.LogInformation("Request {requestName} handled", nameof(DeleteLanguages));

            return StatusCode((int)HttpStatusCode.OK);
        }

        [SwaggerOperation(Summary = "Assign skill(s) to the specified opportunity")]
        [HttpPut("{id}/assign/skills")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> AssignSkills([FromRoute] Guid id, [FromBody] List<Guid> skillIds)
        {
            _logger.LogInformation("Handling request {requestName}", nameof(AssignSkills));

            await _opportunityService.AssignSkills(id, skillIds);

            _logger.LogInformation("Request {requestName} handled", nameof(AssignSkills));

            return StatusCode((int)HttpStatusCode.OK);
        }

        [SwaggerOperation(Summary = "Remove skill(s) from the specified opportunity")]
        [HttpDelete("{id}/remove/skills")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteSkills([FromRoute] Guid id, [FromBody] List<Guid> skillIds)
        {
            _logger.LogInformation("Handling request {requestName}", nameof(DeleteSkills));

            await _opportunityService.DeleteSkills(id, skillIds);

            _logger.LogInformation("Request {requestName} handled", nameof(DeleteSkills));

            return StatusCode((int)HttpStatusCode.OK);
        }
        #endregion Administrative Actions
        #endregion
    }
}
