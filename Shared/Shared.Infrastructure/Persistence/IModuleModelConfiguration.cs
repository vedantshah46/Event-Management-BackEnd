using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Shared.Infrastructure.Persistence;

public interface IModuleModelConfiguration
{
    void Configure(ModelBuilder builder);
}

public sealed class ModuleModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) => context is EventProjectDbContext modular
        ? (context.GetType(), modular.ModuleModelKey, designTime)
        : (object)(context.GetType(), designTime);
}
