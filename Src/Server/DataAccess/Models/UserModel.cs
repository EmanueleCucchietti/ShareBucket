using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DataAccess.Models;

[Table("User")]
[Index(nameof(Id), nameof(Email), IsUnique = true)]
    public class UserModel
    {
        public int Id { get; set; }
        [Required]
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        [JsonIgnore]
        public string PasswordHash { get; set; }
        [JsonIgnore]
        public string PasswordSalt { get; set; }
        public int UsedMemory { get; set; } = 0;


        // Relationships
        public List<MemoryAreaModel>? MemoryAreasPartecipated { get; set; }
        public List<FriendshipModel>? Friendships { get; set; }
    }
