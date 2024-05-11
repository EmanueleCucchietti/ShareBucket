using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DataAccess.Models
{
    [Table("Tier")]
    [Index(nameof(Id), IsUnique = true)]
    public class TierModel
    {
        public int Id { get; set; }
        [MaxLength(50), Required]
        public string Name { get; set; }
        [Required]
        public int MaxGB { get; set; }
        [Required]
        public int MaxAreaNumber { get; set; }
        [Required]
        public decimal Price { get; set; }
    }
}