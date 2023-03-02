﻿using System.Net.Mime;
using Altairis.Services.DateProvider;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using static Altairis.Incitatus.Web.Api.SitesController;

namespace Altairis.Incitatus.Web.Api;

[ApiController, Route("/api/search"), ApiExplorerSettings(GroupName = "search")]
[Produces(MediaTypeNames.Application.Json), Consumes(MediaTypeNames.Application.Json)]
public class SearchController : ControllerBase {
    private readonly IncitatusDbContext dc;
    private readonly IDateProvider dateProvider;
    private readonly ILogger<SitesController> logger;

    // Constructor

    public SearchController(IncitatusDbContext dc, IDateProvider dateProvider, ILogger<SitesController> logger) {
        this.dc = dc;
        this.dateProvider = dateProvider;
        this.logger = logger;
    }

    // Action methods

    /// <summary>
    /// Gets search results for specified site.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="query">The search query</param>
    /// <returns></returns>
    [HttpGet("{siteId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<SearchResultModel>>> Get(Guid siteId, string query) {
        try {
            var result = await this.dc.SearchStories(siteId, query).Select(x => new SearchResultModel(x.Url, x.Title, x.Description, x.DateLastUpdated)).ToListAsync();
            return this.Ok(result);
        } catch (SqlException sex) when (sex.Number == 7645) { // Null or empty full-text predicate.
            return this.BadRequest();
        }
    }

    public record SearchResultModel(string Url, string Title, string Description, DateTime? LastUpdated);

}