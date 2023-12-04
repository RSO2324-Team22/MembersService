namespace MembersService.Database;

public class MembersDbContext : DbContext {
    private readonly ILogger<MembersDbContext> _logger;

    public DbSet<Member> Members { get; private set; }

    public MembersDbContext(
            DbContextOptions<MembersDbContext> options,
            ILogger<MembersDbContext> logger) : base(options) {
        this._logger = logger;
    }
}
