namespace MembersService.Members;

public class CreateMemberModel {
    public required string Name { get; set; }
    public required string Section { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public IEnumerable<string> Roles { get; set; } = new List<string>();
}
