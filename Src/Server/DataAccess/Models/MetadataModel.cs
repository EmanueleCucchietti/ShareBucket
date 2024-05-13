using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Models;

[Table("Metadata")]
[Index(nameof(Id), IsUnique = true)]
public class MetadataModel
{
    public int Id { get; set; }
    [Required]
    public int Path { get; set; }
    [Required]
    public string Filename { get; set; }
    [Required]
    public string FileExtension { get; set; }
    public DateTime DataCreation { get; set; } = DateTime.Now;


    // Relationships
    public int MemoryAreaId { get; set; }
    public MemoryAreaModel? MemoryArea { get; set; }
}
