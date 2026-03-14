namespace IdentityService.Models.DTOs
{
    public class ChangeRoleRequest
    {
        public Guid Id { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}
