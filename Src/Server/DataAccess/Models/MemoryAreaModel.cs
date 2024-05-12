using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Models;

[Table("MemoryArea")]
[Index(nameof(Id), IsUnique = true)]
public class MemoryAreaModel
{
    public int Id { get; set; }
    [Required]
    public int MaxGB { get; set; }
    public DateTime CreationDate { get; set; }
    public byte[] EncryptionKey { get; set; }

    // Relationships
    public List<UserModel>? Users { get; set; }
}