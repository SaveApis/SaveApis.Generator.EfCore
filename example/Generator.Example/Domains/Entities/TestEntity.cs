using SaveApis.Generator.EfCore.Infrastructure.Persistence.Sql.Entities.Attributes;

namespace Generator.Example.Domains.Entities;

[Entity]
public partial class TestEntity
{
    public Guid Id { get; }
    public string Test { get; }
    public int TestInt { get; private set; }
}
