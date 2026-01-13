using Compass.Models.Fips;

namespace Compass.Services.Fips;

public interface IStrapiService
{
    Task<List<StrapiProduct>> GetAllProductsAsync();
    Task<int> GetProductCountAsync();
    Task<StrapiProduct?> FindProductByCmdbSysIdAsync(string cmdbSysId);
    Task<StrapiProduct> CreateProductAsync(CmdbEntry cmdbEntry);
    Task<StrapiProduct> UpdateProductAsync(string documentId, CmdbEntry cmdbEntry, StrapiProduct existingProduct);
    Task DeleteProductAsync(string documentId);
}
