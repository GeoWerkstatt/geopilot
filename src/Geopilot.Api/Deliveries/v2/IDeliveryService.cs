using Geopilot.Api.Contracts;
using Geopilot.Api.Exceptions;
using System.Security.Claims;

namespace Geopilot.Api.Deliveries.V2;

/// <summary>
/// Defines the contract for the V2 delivery service.
/// </summary>
public interface IDeliveryService
{
    /// <summary>
    /// Creates a complete V2 delivery by validating preconditions,
    /// archiving assets, and creating database records.
    /// </summary>
    /// <param name="request">The delivery request from the user.</param>
    /// <param name="userPrincipal">The user performing the action.</param>
    /// <returns>The final, persisted Delivery object.</returns>
    /// <exception cref="DeliveryValidationException">Thrown when business rules are violated.</exception>
    /// <exception cref="ValidationJobNotFoundException">Thrown when the V2 job doesn't exist or isn't complete.</exception>
    Task<Models.Delivery> CreateV2DeliveryAsync(DeliveryRequest request, ClaimsPrincipal userPrincipal);
}
