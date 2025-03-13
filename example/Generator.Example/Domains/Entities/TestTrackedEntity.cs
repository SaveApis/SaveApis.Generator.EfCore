using SaveApis.Generator.EfCore.Infrastructure.Persistence.Sql.Entities;

namespace Generator.Example.Domains.Entities;

public partial class TestTrackedEntity : ITrackedEntity<Guid>
{
    public Guid Id { get; }
    public string Test { get; private set; }
}
