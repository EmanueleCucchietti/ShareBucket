using DataAccess.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models;

[Table("Partecipate")]
public class PartecipateModel
{
    // Relationship between User and MemoryArea
    public int UserId { get; set; }
    public int MemoryAreaId { get; set; }
    public DateTime StartDate { get; set; }
}
