using Asp.Versioning;
using Geopilot.Api.Authorization;
using Geopilot.Api.Contracts;
using Geopilot.Api.Deliveries.V2;
using Geopilot.Api.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Geopilot.Api.Controllers.V2;

/// <summary>
/// API controller for creating V2 deliveries.
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/delivery")]
public class DeliveryV2Controller : ControllerBase
{
    private readonly IDeliveryService deliveryService;
    private readonly ILogger<DeliveryV2Controller> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryV2Controller"/> class.
    /// </summary>
    /// <param name="deliveryService">The service responsible for creating the delivery.</param>
    /// <param name="logger">The logger for recording events and errors.</param>
    public DeliveryV2Controller(IDeliveryService deliveryService, ILogger<DeliveryV2Controller> logger)
    {
        this.deliveryService = deliveryService;
        this.logger = logger;
    }

    /// <summary>
    /// Creates a new delivery based on a completed validation job.
    /// </summary>
    /// <param name="declaration">The delivery request, including the job ID, mandate, and other metadata.</param>
    /// <returns>An HTTP 201 Created response with the details of the new delivery.</returns>
    /// <response code="201">Returns the newly created delivery information and a `Location` header pointing to its resource URI.</response>
    /// <response code="400">If the delivery request fails business rule validation (e.g., missing comment, invalid precursor).</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not authorized to perform a delivery for the specified mandate.</response>
    /// <response code="404">If the specified validation job is not found or not completed.</response>
    /// <response code="500">If an unexpected server error occurs.</response>
    [HttpPost]
    [Authorize(Policy = GeopilotPolicies.User)]
    public async Task<IActionResult> Create(DeliveryRequest declaration)
    {
        try
        {
            var createdDelivery = await deliveryService.CreateV2DeliveryAsync(declaration, User);
            var response = MapToResponse(createdDelivery);
            var location = new Uri($"/api/v1/delivery/{createdDelivery.Id}", UriKind.Relative);
            return Created(location, response);
        }
        catch (ValidationJobNotFoundException ex)
        {
            logger.LogWarning(ex, "Could not create delivery for job {JobId}", declaration.JobId);
            return NotFound(new ProblemDetails { Title = "Job Not Found", Detail = ex.Message });
        }
        catch (DeliveryValidationException ex)
        {
            logger.LogWarning(ex, "Business rule validation failed for job {JobId}", declaration.JobId);
            return BadRequest(new ProblemDetails { Title = "Validation Failed", Detail = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error occurred while creating delivery for job {JobId}", declaration.JobId);
            return Problem("An unexpected error occurred.");
        }
    }

