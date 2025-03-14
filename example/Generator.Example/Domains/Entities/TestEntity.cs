﻿using SaveApis.Generator.EfCore.Infrastructure.Persistence.Sql.Entities;

namespace Generator.Example.Domains.Entities;

public partial class TestEntity : IEntity<Guid>
{
    public Guid Id { get; }
    public string Test { get; }
    public int TestInt { get; private set; }
}
