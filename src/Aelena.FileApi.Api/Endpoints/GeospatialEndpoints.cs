namespace Aelena.FileApi.Api.Endpoints;

/// <summary>Geospatial feature extraction endpoints — KML, KMZ, GeoJSON, Shapefile, DXF.</summary>
public static class GeospatialEndpoints
{
    private static readonly string[] SupportedExtensions =
        ["kml", "kmz", "geojson", "shp", "dxf", "geotiff"];

    public static RouteGroupBuilder MapGeospatialEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/extract", (IFormFile file) =>
            Stub("Geospatial extraction requires NetTopologySuite. Coming in Phase 7 implementation.")
        ).WithName("GeospatialExtract").DisableAntiforgery();

        group.MapPost("/metrics", (IFormFile file) =>
            Stub("Geospatial metrics requires NetTopologySuite. Coming in Phase 7 implementation.")
        ).WithName("GeospatialMetrics").DisableAntiforgery();

        group.MapPost("/extract-text", (IFormFile file) =>
            Stub("Geospatial text extraction requires NetTopologySuite. Coming in Phase 7 implementation.")
        ).WithName("GeospatialExtractText").DisableAntiforgery();

        group.MapGet("/formats", () => Results.Ok(new
        {
            supported_extensions = SupportedExtensions,
            notes = "Geospatial processing uses NetTopologySuite + BitMiracle.LibTiff.NET. DWG support is partial (DXF only via netDxf)."
        })).WithName("GeospatialFormats");

        return group;
    }

    private static IResult Stub(string msg) => throw new FileApiException(501, msg);
}
