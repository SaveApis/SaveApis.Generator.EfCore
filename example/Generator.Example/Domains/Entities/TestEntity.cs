using Generator.Example.Infrastructure.Persistence.Sql.Entities;

namespace Generator.Example.Domains.Entities;

public partial class TestEntity : IEntity
{
    public Guid Id { get; }
    public string Test { get; }
    public int TestInt { get; private set; }
    public string Class { get; }
    public string Namespace { get; private set; }
}
