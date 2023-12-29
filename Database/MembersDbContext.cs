using MembersService.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MembersService.Database;

public class MembersDbContext : DbContext {
    private readonly ILogger<MembersDbContext> _logger;

    public DbSet<Member> Members { get; private set; }

    public MembersDbContext(
            DbContextOptions<MembersDbContext> options,
            ILogger<MembersDbContext> logger) : base(options) {
        this._logger = logger;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Member>()
            .Property(e => e.Roles)
            .HasConversion(new EnumCollectionJsonValueConverter<Role>())
            .Metadata.SetValueComparer(new CollectionValueComparer<Role>());
    }
}

class EnumCollectionJsonValueConverter<T> : ValueConverter<IEnumerable<T>, string> 
where T : Enum
{
    public EnumCollectionJsonValueConverter() : base(
        v => JsonConvert
            .SerializeObject(v.Select(e => e.ToString()).ToList()),
        v => JsonConvert
            .DeserializeObject<IEnumerable<string>>(v)
            .Select(e => Enum.Parse<T>(e)).ToHashSet()) {}
}

class CollectionValueComparer<T> : ValueComparer<IEnumerable<T>>
{
    public CollectionValueComparer() : base(
        (c1, c2) => c1.SequenceEqual(c2),
        c => c.Aggregate(0, 
            (a, v) => HashCode.Combine(a, v.GetHashCode())), 
                c => (IEnumerable<T>)c.ToHashSet()) {}
}
