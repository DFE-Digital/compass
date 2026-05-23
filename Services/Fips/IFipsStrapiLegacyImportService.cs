namespace Compass.Services.Fips;

public interface IFipsStrapiLegacyImportService
{
    /// <summary>
    /// Bulk-update service register products from a legacy Strapi/CMS export JSON file.
    /// Matches rows by <c>cmdb_sys_id</c> to <see cref="Models.Fips.CMDBProduct.CMDBID"/>.
    /// Updates user description, product URL, phase, channel, and type — not business area.
    /// </summary>
    Task<FipsCompletionImportResult> ImportAsync(
        Stream jsonStream,
        string actorEmail,
        string? auditDisplayName,
        bool dryRun = false,
        CancellationToken cancellationToken = default);
}
