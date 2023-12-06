using System.ComponentModel.DataAnnotations.Schema;

namespace MembersService.Members;

public class Member {
    public int Id { get; private set; }
    public required string Name { get; set; }
    public required Section Section { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    [NotMapped]
    public IEnumerable<Role> Roles { get; init; } = new List<Role>();
}
