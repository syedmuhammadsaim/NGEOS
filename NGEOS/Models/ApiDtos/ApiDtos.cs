namespace NGEOS.Models.ApiDtos
{
    public class LoginRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // "Admin" or "User"
        public string? ProfileImageUrl { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Profession { get; set; }
        public string? Organization { get; set; }
    }

    public class AdminDashboardDto
    {
        public decimal TotalDonations { get; set; }
        public int TotalDonors { get; set; }
        public int TotalCampaigns { get; set; }
        public int TotalBeneficiaries { get; set; }
        public int TotalProjects { get; set; }
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        
        public List<MonthlyDonationChartItem> MonthlyChart { get; set; } = new();
        public List<CategoryDonationItem> CategoryStats { get; set; } = new();
        public List<RecentTransactionDto> RecentTransactions { get; set; } = new();
        public List<RecentActivityDto> RecentActivities { get; set; } = new();
    }

    public class MonthlyDonationChartItem
    {
        public string Month { get; set; } = string.Empty; // "Jan", "Feb", etc.
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    public class CategoryDonationItem
    {
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    public class RecentTransactionDto
    {
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
    }

    public class RecentActivityDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class UserDashboardDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal TotalDonated { get; set; }
        public int CausesSupportedCount { get; set; }
        public int ProgrammesJoinedCount { get; set; }
        public int QueriesCount { get; set; }
        public List<RecentTransactionDto> RecentDonations { get; set; } = new();
        public List<ProgrammeInterestDto> MyProgrammes { get; set; } = new();
        public List<UserQueryDto> MyQueries { get; set; } = new();
    }

    public class ProgrammeInterestDto
    {
        public int Id { get; set; }
        public int ProgrammeId { get; set; }
        public string ProgrammeTitle { get; set; } = string.Empty;
        public string InterestType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
    }

    public class UserQueryDto
    {
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
    }

    public class SyncPullResponseDto
    {
        public DateTime ServerTimeUtc { get; set; } = DateTime.UtcNow;
        public List<CauseDto> Causes { get; set; } = new();
        public List<ProgrammeDto> Programmes { get; set; } = new();
        public List<RecentTransactionDto> Donations { get; set; } = new();
        public List<UserQueryDto> Queries { get; set; } = new();
    }

    public class CauseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string FullDescription { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public decimal RaisedAmount { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ProgrammeDto
    {
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
        public bool IsActive { get; set; }
    }

    public class SyncPushRequestDto
    {
        public List<OfflinePendingOperationDto> Operations { get; set; } = new();
    }

    public class OfflinePendingOperationDto
    {
        public string OperationId { get; set; } = Guid.NewGuid().ToString("N");
        public string EntityType { get; set; } = string.Empty; // "Donation", "Query", "ProgrammeInterest"
        public string ActionType { get; set; } = string.Empty; // "Create", "Update"
        public string JsonPayload { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }

    public class SyncPushResultDto
    {
        public string OperationId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? ServerEntityId { get; set; }
    }
}
