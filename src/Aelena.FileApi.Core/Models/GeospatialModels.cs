namespace Aelena.FileApi.Core.Models;

/// <summary>A single geospatial feature extracted from a file.</summary>
public sealed record GeospatialFeature(
    string FeatureType,
    string TextDescription,
    string? Name = null,
    string? Description = null,
    string? GeometryType = null,
    string? Coordinates = null,
    string? Centroid = null,
    IReadOnlyDictionary<string, string>? Attributes = null,
    string? Layer = null);

/// <summary>Full extraction result from a geospatial file.</summary>
public sealed record GeospatialExtractionResponse(
    string FileName,
    string FileType,
    int FeatureCount,
    IReadOnlyList<GeospatialFeature> Features,
    string? SpatialReference = null,
    IReadOnlyDictionary<string, double>? Bounds = null,
    IReadOnlyList<string>? Layers = null);

/// <summary>Metrics for a geospatial file.</summary>
public sealed record GeospatialMetrics(
    string FileName,
    long FileSizeBytes,
    string FileType,
    int FeatureCount,
    IReadOnlyList<string> GeometryTypes,
    IReadOnlyList<string>? Layers = null,
    string? SpatialReference = null,
    IReadOnlyDictionary<string, double>? Bounds = null,
    int TextChunkCount = 0);
