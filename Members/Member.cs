namespace MembersService.Members;

public class Member {
    public int Id { get; set; }
    public required string Name { get; set; }
    public required Section Section { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public IEnumerable<Role> Roles { get; set; } = new List<Role>();
}
