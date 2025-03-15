using MediatR;
using Microsoft.EntityFrameworkCore;
using SaveApis.Common.Infrastructure.Persistence.Sql;

namespace Generator.Example.Persistence.Sql;

public class TestDbContext(DbContextOptions options, IMediator mediator) : BaseDbContext(options, mediator)
{
    protected override string Schema => "Test";

    protected override void RegisterEntities(ModelBuilder modelBuilder)
    {
    }
}
