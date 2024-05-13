using DataAccess.Models;

namespace ApiApp.Models.Dto.User;

public class UserUpdateDto
{
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int UsedMemory { get; set; } = 0;


    // Relationships
    public List<MemoryAreaModel>? MemoryAreasPartecipated { get; set; }
    public List<FriendshipModel>? Friendships { get; set; }
}
