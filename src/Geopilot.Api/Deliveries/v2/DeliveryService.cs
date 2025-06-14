using Geopilot.Api.Contracts;
using Geopilot.Api.Exceptions;
using Geopilot.Api.FileAccess;
using Geopilot.Api.FileAccess.V2;
using Geopilot.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Geopilot.Api.Deliveries.V2;

/// <summary>
/// Service responsible for orchestrating the creation of a V2 delivery.
/// This service validates business rules, archives files from blob storage to a permanent location,
/// and creates the corresponding database entries for the delivery and its assets.
/// </summary>
public class DeliveryService : IDeliveryService
{
    private readonly Context context;
    private readonly IBlobStorageService blobStorageService;
    private readonly IDirectoryProvider directoryProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryService"/> class.
    /// </summary>
    /// <param name="context">The database context for data access.</param>
    /// <param name="blobStorageService">The service for accessing Azure Blob Storage.</param>
    /// <param name="directoryProvider">The provider for resolving file system paths.</param>
    public DeliveryService(
        Context context,
        IBlobStorageService blobStorageService,
        IDirectoryProvider directoryProvider)
    {
        this.context = context;
        this.blobStorageService = blobStorageService;
        this.directoryProvider = directoryProvider;
    }

    /// <inheritdoc />
    public async Task<Delivery> CreateV2DeliveryAsync(DeliveryRequest request, ClaimsPrincipal userPrincipal)
    {
        var (mandate, user, precursor) = await ValidatePreconditionsAsync(request, userPrincipal);

        var v2Job = await ValidateV2JobAsync(request.JobId);
        var assets = await PersistV2AssetsAsync(v2Job);

        var delivery = new Delivery
        {
            JobId = request.JobId,
            Mandate = mandate,
            DeclaringUser = user,
            PrecursorDelivery = precursor,
            Partial = request.PartialDelivery,
            Comment = request.Comment?.Trim() ?? string.Empty,
            Assets = assets
        };

        context.Deliveries.Add(entity: delivery);
        await context.SaveChangesAsync();

        return delivery;
    }

    /// <summary>
    /// Validates the business logic preconditions for creating a delivery.
    /// This includes checking user authorization, mandate rules, and precursor/partial/comment requirements.
    /// </summary>
    /// <param name="request">The incoming delivery request.</param>
    /// <param name="userPrincipal">The principal of the user making the request.</param>
    /// <returns>A tuple containing the validated <see cref="Mandate"/>, <see cref="User"/>, and optional precursor <see cref="Models.Delivery"/>.</returns>
    /// <exception cref="DeliveryValidationException">Thrown if any business rule is violated.</exception>
    private async Task<(Mandate Mandate, User User, Delivery? Precursor)> ValidatePreconditionsAsync(DeliveryRequest request, ClaimsPrincipal userPrincipal)
    {
        var user = await context.GetUserByPrincipalAsync(userPrincipal)
            ?? throw new DeliveryValidationException("User not found.");

        var mandate = await context.Mandates
            .Include(m => m.Organisations)
            .ThenInclude(o => o.Users)
            .Include(m => m.Deliveries)
            .SingleOrDefaultAsync(m => m.Id == request.MandateId)
            ?? throw new DeliveryValidationException($"Mandate with id <{request.MandateId}> not found.");

        if (mandate.Organisations.SelectMany(u => u.Users).All(u => u.Id != user.Id))
        {
            throw new DeliveryValidationException($"User is not authorized for mandate <{request.MandateId}>.");
        }

        if (mandate.EvaluatePrecursorDelivery == FieldEvaluationType.Required && !request.PrecursorDeliveryId.HasValue)
            throw new DeliveryValidationException("Precursor delivery is required for this mandate.");
        if (mandate.EvaluatePrecursorDelivery == FieldEvaluationType.NotEvaluated && request.PrecursorDeliveryId.HasValue)
            throw new DeliveryValidationException("Precursor delivery is not allowed for this mandate.");

        Delivery? precursorDelivery = null;
        if (request.PrecursorDeliveryId.HasValue)
        {
            precursorDelivery = mandate.Deliveries.SingleOrDefault(d => d.Id == request.PrecursorDeliveryId.Value)
                ?? throw new DeliveryValidationException("Precursor delivery not found.");
        }

        if (mandate.EvaluatePartial == FieldEvaluationType.Required && !request.PartialDelivery.HasValue)
            throw new DeliveryValidationException("Partial delivery is required for this mandate.");
        if (mandate.EvaluatePartial == FieldEvaluationType.NotEvaluated && request.PartialDelivery.HasValue)
            throw new DeliveryValidationException("Partial delivery is not allowed for this mandate.");

        if (mandate.EvaluateComment == FieldEvaluationType.Required && string.IsNullOrWhiteSpace(request.Comment))
            throw new DeliveryValidationException("Comment is required for this mandate.");
        if (mandate.EvaluateComment == FieldEvaluationType.NotEvaluated && !string.IsNullOrWhiteSpace(request.Comment))
            throw new DeliveryValidationException("Comment is not allowed for this mandate.");

        return (mandate, user, precursorDelivery);
    }

    /// <summary>
    /// Ensures the specified <see cref="ValidationJob"/> exists and is in the 'Completed' state.
    /// </summary>
    /// <param name="jobId">The ID of the validation job to check.</param>
    /// <returns>The validated <see cref="ValidationJob"/> with its files and logs included.</returns>
    /// <exception cref="ValidationJobNotFoundException">Thrown if the job does not exist or is not complete.</exception>
    private async Task<ValidationJob> ValidateV2JobAsync(Guid jobId)
    {
        var v2Job = await context.ValidationJobs
            .AsNoTracking() // We don't need to track this entity, just read it.
            .Include(j => j.Files) // IMPORTANT: Load the associated files.
            .ThenInclude(f => f.Logs)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (v2Job == null)
        {
            throw new ValidationJobNotFoundException($"V2 job {jobId} not found.");
        }
        if (v2Job.Status != ValidationJobStatus.Completed)
        {
            throw new ValidationJobNotFoundException($"V2 job {jobId} is not complete. Current status: {v2Job.Status}.");
        }

        return v2Job;
    }
}
