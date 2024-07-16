﻿using Geopilot.Api.Models;

namespace Geopilot.Api.DTOs;

/// <summary>
/// A contract between the system owner and an organisation for data delivery.
/// The mandate describes where and in what format data should be delivered.
/// </summary>
public class MandateDto
{
    /// <summary>
    /// Create a new <see cref="MandateDto"/> from a <see cref="Mandate"/>.
    /// </summary>
    public static MandateDto FromMandate(Mandate mandate)
    {
        var wkt = mandate.SpatialExtent.AsText();
        var spatialExtent = new List<CoordinateDto>();
        if (mandate.SpatialExtent.Coordinates.Length == 5)
        {
            spatialExtent.Add(CoordinateDto.FromCoordinate(mandate.SpatialExtent.Coordinates[0]));
            spatialExtent.Add(CoordinateDto.FromCoordinate(mandate.SpatialExtent.Coordinates[2]));
        }

        return new MandateDto
        {
            Id = mandate.Id,
            Name = mandate.Name,
            FileTypes = mandate.FileTypes,
            SpatialExtent = spatialExtent,
            Organisations = mandate.Organisations.Select(o => o.Id).ToList(),
            Deliveries = mandate.Deliveries.Select(d => d.Id).ToList(),
        };
    }

    /// <summary>
    /// The unique identifier for the mandate.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The display name of the mandate.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// List of file types that are allowed to be delivered. Include the period "." and support wildcards "*".
    /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
    public string[] FileTypes { get; set; } = Array.Empty<string>();
#pragma warning restore CA1819 // Properties should not return arrays

    /// <summary>
    /// The minimum and maximum coordinates of the spatial extent of the mandate.
    /// </summary>
    public List<CoordinateDto> SpatialExtent { get; set; } = new List<CoordinateDto>();

    /// <summary>
    /// IDs of the organisations allowed to deliver data fulfilling the mandate.
    /// </summary>
    public List<int> Organisations { get; set; } = new List<int>();

    /// <summary>
    /// IDs of the data deliveries that have been declared fulfilling the mandate.
    /// </summary>
    public List<int> Deliveries { get; set; } = new List<int>();
}