﻿using System.Globalization;
using GeoCop.Api.Authorization;
using GeoCop.Api.Contracts;
using GeoCop.Api.FileAccess;
using GeoCop.Api.Models;
using GeoCop.Api.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace GeoCop.Api.Controllers;

/// <summary>
/// Controller for declaring deliveries.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class DeliveryController : ControllerBase
{
    private readonly ILogger<DeliveryController> logger;
    private readonly Context context;
    private readonly IValidationService validatorService;
    private readonly IAssetHandler assetHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryController"/> class.
    /// </summary>
    public DeliveryController(ILogger<DeliveryController> logger, Context context, IValidationService validatorService, IAssetHandler assetHandler)
    {
        this.logger = logger;
        this.context = context;
        this.validatorService = validatorService;
        this.assetHandler = assetHandler;
    }

    /// <summary>
    /// Create a delivery from a validation with the status <see cref="Status.Completed"/>.
    /// </summary>
    /// <param name="declaration"><see cref="DeliveryRequest"/> containing all information for the declaration process.</param>
    /// <returns>Created <see cref="Delivery"/>.</returns>
    [HttpPost]
    [Authorize(Policy = GeocopPolicies.User)]
    [SwaggerResponse(StatusCodes.Status201Created, "The delivery was created successfully.")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "The server cannot process the request due to invalid or malformed request.", typeof(ValidationProblemDetails), new[] { "application/json" })]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The validation job or mandate could not be found.")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "The server encountered an unexpected condition that prevented it from fulfilling the request.", typeof(ProblemDetails), new[] { "application/json" })]
    public async Task<IActionResult> Create(DeliveryRequest declaration)
    {
        logger.LogTrace("Declaration for job <{JobId}> requested.", declaration.JobId);

        var jobStatus = validatorService.GetJobStatus(declaration.JobId);
        if (jobStatus == default)
        {
            logger.LogTrace("No job information available for job id <{JobId}>.", declaration.JobId);
            return NotFound($"No job information available for job id <{declaration.JobId}>");
        }
        else if (jobStatus.Status != Status.Completed)
        {
            logger.LogTrace("Job <{JobId}> is not completed.", declaration.JobId);
            return BadRequest($"Job <{declaration.JobId}> is not completed.");
        }

        var user = await context.GetUserByPrincipalAsync(User);
        var mandate = context.DeliveryMandates
            .Include(m => m.Organisations)
            .ThenInclude(o => o.Users)
            .FirstOrDefault(m => m.Id == declaration.DeliveryMandateId);

        if (user is null)
            return Unauthorized();

        if (mandate is null || !mandate.Organisations.SelectMany(u => u.Users).Any(u => u.Id == user.Id))
        {
            logger.LogTrace("User <{AuthIdentifier}> is not authorized to create a delivery for mandate <{MandateId}>.", user.AuthIdentifier, declaration.DeliveryMandateId);
            return NotFound($"Mandate with id <{declaration.DeliveryMandateId}> not found or user is not authorized.");
        }

        var precursorDelivery = declaration.PrecursorDeliveryId.HasValue ?
            context.Deliveries
            .Include(d => d.DeliveryMandate)
            .Where(d => d.DeliveryMandate.Id == mandate.Id)
            .FirstOrDefault(d => d.Id == declaration.PrecursorDeliveryId) : null;

        if (declaration.PrecursorDeliveryId.HasValue && precursorDelivery is null)
        {
            logger.LogTrace("Precursor delivery with Id <{DeliveryId}> was not found or user <{AuthIdentifier}> is not authorized.", declaration.PrecursorDeliveryId.Value, user.AuthIdentifier);
            return NotFound($"Precursor delivery with Id {declaration.PrecursorDeliveryId} not found or user is not authorized.");
        }

        var delivery = new Delivery
        {
            JobId = declaration.JobId,
            DeliveryMandate = mandate,
            DeclaringUser = user,
            PrecursorDelivery = precursorDelivery,
            Partial = declaration.PartialDelivery,
            Comment = declaration.Comment?.Trim() ?? string.Empty,
            Assets = new List<Asset>(),
        };

        try
        {
            delivery.Assets.AddRange(assetHandler.PersistJobAssets(declaration.JobId));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while persisting assets for job <{JobId}>.", declaration.JobId);
            return Problem($"Error while persisting assets for job <{declaration.JobId}>.");
        }

        var entityEntry = context.Deliveries.Add(delivery);
        context.SaveChanges();

        var resultDelivery = context.Deliveries
            .AsNoTracking()
            .FirstOrDefault(d => d.Id == entityEntry.Entity.Id);

        var location = new Uri(
            string.Format(CultureInfo.InvariantCulture, "/api/v1/delivery/{0}", entityEntry.Entity.Id),
            UriKind.Relative);

        return Created(location, resultDelivery);
    }

    /// <summary>
    /// Gets a filtered list of deliveries accessible for the user.
    /// </summary>
    /// <param name="mandateId">Optional. Filter deliveries for given mandate.</param>
    /// <returns>A list of <see cref="Delivery"/>.</returns>
    [HttpGet]
    [Authorize(Policy = GeocopPolicies.User)]
    [SwaggerResponse(StatusCodes.Status200OK, "A list matching filter criteria.", typeof(List<Delivery>), new[] { "application/json" })]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Failed to find mandate.")]
    public async Task<IActionResult> Get([FromQuery] int? mandateId = null)
    {
        var user = await context.GetUserByPrincipalAsync(User);
        if (user == null)
            return Unauthorized();

        var userMandatesIds = context.DeliveryMandates
            .Where(m => user.IsAdmin || m.Organisations.SelectMany(o => o.Users).Any(u => u.Id == user.Id))
            .Select(m => m.Id)
            .ToList();

        if (mandateId.HasValue && !userMandatesIds.Contains(mandateId.Value))
            return NotFound();

        var result = context.DeliveriesWithIncludes
            .Where(d => userMandatesIds.Contains(d.DeliveryMandate.Id));

        if (mandateId.HasValue)
        {
            result = result.Where(d => d.DeliveryMandate.Id == mandateId.Value);
        }

        return Ok(result.ToList());
    }

    /// <summary>
    /// Performs a soft delete in the database and deletes the files from the storage.
    /// </summary>
    /// <returns>An updated list of <see cref="Delivery"/>.</returns>
    [HttpDelete]
    [Route("{deliveryId}")]
    [Authorize(Policy = GeocopPolicies.Admin)]
    [SwaggerResponse(StatusCodes.Status200OK, "The delivery was successfully deleted.")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "The server cannot process the request due to invalid or malformed request.", typeof(ValidationProblemDetails), new[] { "application/json" })]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The delivery could be found.")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "The server encountered an unexpected condition that prevented it from fulfilling the request.", typeof(ProblemDetails), new[] { "application/json" })]
    public IActionResult Delete([FromRoute] int deliveryId)
    {
        try
        {
            var delivery = context.Deliveries.Include(d => d.Assets).FirstOrDefault(d => d.Id == deliveryId);
            if (delivery == default)
            {
                logger.LogTrace($"No delivery with id <{deliveryId}> found.");
                return NotFound($"No delivery with id <{deliveryId}> found.");
            }

            delivery.Deleted = true;
            delivery.Assets.ForEach(a => a.Deleted = true);
            assetHandler.DeleteJobAssets(delivery.JobId);

            context.SaveChanges();

            return Ok();
        }
        catch (Exception e)
        {
            var message = $"Error while deleting delivery <{deliveryId}>.";
            logger.LogError(e, message);
            return Problem(message);
        }
    }

    /// <summary>
    /// Downloads an asset from the persistent storage.
    /// </summary>
    /// <returns>The asset file.</returns>
    [HttpGet]
    [Route("assets/{assetId}")]
    [Authorize(Policy = GeocopPolicies.Admin)]
    [SwaggerResponse(StatusCodes.Status200OK, "A file has been downloaded.", typeof(File), new[] { "application/json" })]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "The server cannot process the request due to invalid or malformed request.", typeof(ValidationProblemDetails), new[] { "application/json" })]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The asset could be found.")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "The server encountered an unexpected condition that prevented it from fulfilling the request.", typeof(ProblemDetails), new[] { "application/json" })]
    public async Task<IActionResult> DownloadAsync([FromRoute] int assetId)
    {
        try
        {
            var asset = context.Assets.Include(a => a.Delivery).FirstOrDefault(a => a.Id == assetId && !a.Deleted);
            if (asset == default)
            {
                logger.LogTrace($"No delivery with id <{assetId}> found.");
                return NotFound($"No delivery with id <{assetId}> found.");
            }

            var (content, contentType) = await assetHandler.DownloadAssetAsync(asset.Delivery.JobId, asset.SanitizedFilename);
            return File(content, contentType, asset.OriginalFilename);
        }
        catch (Exception e)
        {
            var message = $"Error while deleting delivery <{assetId}>.";
            logger.LogError(e, message);
            return Problem(message);
        }
    }
}
