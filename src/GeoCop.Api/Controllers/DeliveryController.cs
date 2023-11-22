﻿using System.Globalization;
using GeoCop.Api.Contracts;
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
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
public class DeliveryController : ControllerBase
{
    private readonly ILogger<DeliveryController> logger;
    private readonly Context context;
    private readonly IValidationService validatorService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryController"/> class.
    /// </summary>
    public DeliveryController(ILogger<DeliveryController> logger, Context context, IValidationService validatorService)
    {
        this.logger = logger;
        this.context = context;
        this.validatorService = validatorService;
    }

    /// <summary>
    /// Create a delivery from a validation with the status <see cref="Status.Completed"/>.
    /// </summary>
    /// <param name="declaration"><see cref="DeliveryRequest"/> containing all information for the declaration process.</param>
    /// <returns>Created <see cref="Delivery"/>.</returns>
    [HttpPost]
    public IActionResult Create(DeliveryRequest declaration)
    {
        logger.LogTrace("Declaration for job <{JobId}> requested.", declaration.JobId);

        var job = validatorService.GetJobStatus(declaration.JobId);
        if (job == default)
        {
            logger.LogTrace("No job information available for job id <{JobId}>.", declaration.JobId);
            return Problem($"No job information available for job id <{declaration.JobId}>", statusCode: StatusCodes.Status404NotFound);
        }
        else if (job.Status != Status.Completed)
        {
            logger.LogTrace("Job <{JobId}> is not completed.", declaration.JobId);
            return Problem($"Job <{declaration.JobId}> is not completed.", statusCode: StatusCodes.Status400BadRequest);
        }

        var mandate = context.DeliveryMandates
            .Include(m => m.Organisations)
            .ThenInclude(o => o.Users)
            .FirstOrDefault(m => m.Id == declaration.DeliveryMandateId);

        var dummyUser = mandate?.Organisations.SelectMany(u => u.Users).First() ?? new User();

        if (mandate is null || !mandate.Organisations.SelectMany(u => u.Users).Any(u => u.AuthIdentifier.Equals(dummyUser.AuthIdentifier, StringComparison.OrdinalIgnoreCase)))
        {
            logger.LogTrace("User <{AuthIdentifier}> is not authorized to create a delivery for mandate <{MandateId}>.", dummyUser, declaration.DeliveryMandateId);
            return Problem("Mandate with id <{declaration.DeliveryMandateId}> not found or user is not authorized.", statusCode: StatusCodes.Status404NotFound);
        }

        var delivery = new Delivery
        {
            DeliveryMandate = mandate,
            DeclaringUser = dummyUser,
            Assets = new List<Asset>(),
        };

        var entityEntry = context.Deliveries.Add(delivery);
        context.SaveChanges();

        var location = new Uri(
            string.Format(CultureInfo.InvariantCulture, "/api/v1/delivery/{0}", entityEntry.Entity.Id),
            UriKind.Relative);

        return Created(location, entityEntry.Entity);
    }

    /// <summary>
    /// Gets all deliveries.
    /// </summary>
    /// <returns>A list of <see cref="Delivery"/>.</returns>
    [HttpGet]
    [SwaggerResponse(StatusCodes.Status200OK, "A list with available deliveries has been returned.", typeof(List<DeliveryDto>), new[] { "application/json" })]
    public List<DeliveryDto> Get()
    {
        var deliveries = context.DeliveriesWithIncludes.Select(d => new DeliveryDto(d.Id, d.Date, d.DeclaringUser.AuthIdentifier, d.DeliveryMandate.Name)).ToList();
        return deliveries;
    }
}
