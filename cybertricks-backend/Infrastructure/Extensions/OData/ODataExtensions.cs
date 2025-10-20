using ct.backend.Features.Stores;
using Microsoft.AspNetCore.OData;
using Microsoft.OData.ModelBuilder;

namespace ct.backend.Infrastructure.Extension.OData;

public static class ODataMinimal
{
    public static IMvcBuilder AddODataSupport(this IServiceCollection services)
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntitySet<StoreDto>("Stores");
        //builder.EntitySet<BrandDto>("Brands");

        var edm = builder.GetEdmModel();
        return services.AddControllers().AddOData(opt =>
            opt.AddRouteComponents("odata", edm)
               .Select()
               .Filter()
               .OrderBy()
               .Expand()
               .Count()
               .SetMaxTop(100));
    }
}
