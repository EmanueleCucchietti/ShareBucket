using DataAccess.Models;
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
    [Table("Transaction")]
    [Index(nameof(Id), IsUnique = true)]
    public class TransactionModel
    {
        public int Id { get; set; }
        [Required]
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        [Required]
        public decimal Price { get; set; }


        // Relationships
        [Required]
        public int TierId { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public int ModifierId { get; set; }

        public TierModel Tier { get; set; }
        public UserModel User { get; set; }
        public ModifierModel Modifier { get; set; }
    }
}
