using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenIddict_Server.Models
{
    [Table("Users")]
    public class Users
    {
        [Key]
        public short Id { get; set; }
        public short UserRoleId { get; set; }

        public string? Username { get; set; } // nullable
        public string Password { get; set; } = null!;

        public string? SecurityToken { get; set; } // nullable
        public string FirstName { get; set; } = null!;
        public string? MiddleName { get; set; } // nullable
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;

        public string? PMUsername { get; set; } // nullable
        public string? PMPassword { get; set; } // nullable
        public string? ConedUsername { get; set; } // nullable
        public string? ConedPassword { get; set; } // nullable
        public string? NGUsername { get; set; } // nullable
        public string? NGPassword { get; set; } // nullable

        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }

        public short CreatedByUserId { get; set; }
        public DateTime CreatedTimeStamp { get; set; }

        public short UpdatedByUserId { get; set; }
        public DateTime UpdatedTimeStamp { get; set; }

        public short? DeletedByUserId { get; set; } // nullable
        public DateTime? DeletedTimeStamp { get; set; } // nullable
    }

}
