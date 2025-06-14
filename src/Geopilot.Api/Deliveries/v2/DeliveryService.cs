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

