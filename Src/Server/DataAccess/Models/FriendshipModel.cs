using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Models;

[Table("Friendship")]
[Index(nameof(Id), IsUnique = true)]
public class FriendshipModel
{
    public int Id { get; set; }

    // Relationships
    public int UserId { get; set; }
    public int FriendId { get; set; }

    public UserModel User { get; set; }
    public UserModel Friend { get; set; }
}
