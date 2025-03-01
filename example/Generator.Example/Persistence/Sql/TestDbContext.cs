using Microsoft.EntityFrameworkCore;
using SaveApis.Common.Infrastructure.Persistence.Sql;

namespace Generator.Example.Persistence.Sql;

public class TestDbContext(DbContextOptions options) : BaseDbContext(options)
{
    protected override void RegisterEntities(ModelBuilder modelBuilder)
    {
    }

    protected override string Schema => "Test";
}
