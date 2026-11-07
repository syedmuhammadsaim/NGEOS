using System.ComponentModel.DataAnnotations;

namespace NGEOS.Desktop.Models
{
    public class LocalUser
    {
        [Key]
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "User"; // "Admin" or "User"
        public string? ProfileImageUrl { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Profession { get; set; }
        public string? Organization { get; set; }
        public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    }

    public class LocalDonation
    {
        [Key]
        public int Id { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string DonorName { get; set; } = string.Empty;
        public string DonorEmail { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "PKR";
        public string PaymentGateway { get; set; } = "Card";
        public string PaymentStatus { get; set; } = "Completed";
        public string CauseTitle { get; set; } = string.Empty;
        public DateTime DonationDate { get; set; }
        public bool IsSynced { get; set; } = true;
    }

    public class LocalCause
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string FullDescription { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public decimal RaisedAmount { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public bool IsSynced { get; set; } = true;
    }

    public class LocalProgramme
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string FullDescription { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Location { get; set; }
        public int TargetBeneficiaries { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsSynced { get; set; } = true;
    }

    public class LocalQuery
    {
        [Key]
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? AdminReply { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }
        public DateTime? RepliedAt { get; set; }
        public bool IsSynced { get; set; } = true;
    }

    public class PendingOperation
    {
        [Key]
        public int Id { get; set; }
        public string OperationId { get; set; } = Guid.NewGuid().ToString("N");
        public string EntityType { get; set; } = string.Empty; // "Query", "Cause", "Programme"
        public string ActionType { get; set; } = string.Empty; // "Create", "Update"
        public string JsonPayload { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public int RetryCount { get; set; } = 0;
        public string Status { get; set; } = "Pending"; // "Pending", "Failed", "Completed"
        public string? ErrorMessage { get; set; }
    }
}
