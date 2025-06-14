using Geopilot.Api.FileAccess.V2;
using Geopilot.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Geopilot.Api.Validation.V2;

/// <summary>
/// Background service that continuously processes validation jobs through their complete lifecycle.
/// Handles virus scanning status checks, file validation using appropriate validators, and job status updates.
/// </summary>
public class ValidationRunnerV2 : BackgroundService
{
    private readonly ILogger<ValidationRunnerV2> logger;
    private readonly IServiceProvider serviceProvider;
    private readonly ValidationRunnerV2Options options;
    private readonly IBlobStorageService blobStorageService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationRunnerV2"/> class.
    /// </summary>
    /// <param name="logger">Logger for tracking validation operations and errors.</param>
    /// <param name="serviceProvider">Service provider for creating scoped database contexts during job processing.</param>
    /// <param name="options">Configuration options including polling intervals and processing behavior.</param>
    /// <param name="blobStorageService">Service for accessing files stored in Azure Blob Storage.</param>
    public ValidationRunnerV2(
        ILogger<ValidationRunnerV2> logger,
        IServiceProvider serviceProvider,
        IOptions<ValidationRunnerV2Options> options,
        IBlobStorageService blobStorageService
    )
    {
        this.logger = logger;
        this.serviceProvider = serviceProvider;
        this.options = options.Value;
        this.blobStorageService = blobStorageService;
    }

}
