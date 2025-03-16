using SaveApis.Generator.EfCore.Infrastructure.Persistence.Sql.Entities.Attributes;

namespace Generator.Example.Domains.Entities;

[Entity]
[TrackedEntity]
public partial class TestTrackedEntity
{
    public Guid Id { get; }
    public string Test { get; private set; }
    public int TestInt { get; private set; }
    
    [IgnoreTracking]
    public string IgnoredTest { get; private set; }
    
    [AnonymizeTracking]
    public string AnonymizedTest { get; private set; }
}
