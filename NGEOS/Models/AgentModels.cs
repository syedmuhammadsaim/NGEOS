using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NGEOS.Models
{
    /// <summary>
    /// Global Autonomous Agent ecosystem configuration, provider selection, and kill-switch state.
    /// </summary>
    public class AgentConfiguration
    {
        public int Id { get; set; }

        public bool IsAdminAgentEnabled { get; set; } = true;
        public bool IsUserAgentEnabled { get; set; } = true;
        public bool IsAutomationPaused { get; set; } = false;
        public bool IsEmergencyKillSwitchActive { get; set; } = false;

        [Required, MaxLength(50)]
        public string PrimaryProvider { get; set; } = "Groq";

        [Required, MaxLength(50)]
        public string FallbackProvider { get; set; } = "Gemini";

        [Required, MaxLength(100)]
        public string GroqModel { get; set; } = "llama-3.3-70b-versatile";

        [Required, MaxLength(100)]
        public string GeminiModel { get; set; } = "gemini-2.0-flash";

        public int DonationMonitoringIntervalMinutes { get; set; } = 15;
        public int SecurityScanIntervalMinutes { get; set; } = 10;
        public int HealthCheckIntervalMinutes { get; set; } = 5;

        public DateTime LastSuccessfulExecution { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Tamper-resistant, append-only operational audit report recording every consequential action taken by agents.
    /// </summary>
    public class AgentReport
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string ReportCode { get; set; } = string.Empty; // e.g. "RPT-202611-0001"

        [Required, MaxLength(100)]
        public string AgentName { get; set; } = "NGEOS Admin Agent";

        [Required, MaxLength(50)]
        public string AgentType { get; set; } = "AdminAgent"; // "AdminAgent", "UserAgent", "SecurityEngine", "AutomationEngine"

        [Required, MaxLength(100)]
        public string ActionName { get; set; } = string.Empty; // e.g. "MonitorDonations", "FlagSuspiciousActivity", "ApplyRestriction"

        [Required, MaxLength(100)]
        public string ModuleName { get; set; } = "DonationIntelligence"; // "DonationIntelligence", "UserManagement", "Security", "CampaignManagement", "HealthMonitoring"

        public int? TargetUserId { get; set; }
        [ForeignKey("TargetUserId")]
        public virtual User? TargetUser { get; set; }

        public int? AffectedRecordId { get; set; }

        [Required, MaxLength(1000)]
        public string ActionDescription { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? ActionReason { get; set; }

        public string? PreviousValue { get; set; }
        public string? NewValue { get; set; }

        [Required, MaxLength(20)]
        public string RiskLevel { get; set; } = "Low"; // "Low", "Medium", "High", "Critical"

        [Required, MaxLength(30)]
        public string ActionStatus { get; set; } = "Success"; // "Success", "Failed", "RequiresApproval", "Blocked"

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public double ExecutionDurationMs { get; set; } = 0.0;

        [MaxLength(100)]
        public string? RelatedSecurityEventId { get; set; }

        public string? ErrorDetails { get; set; }

        // Integrity verification hash to guard against audit tampering
        [MaxLength(128)]
        public string IntegrityHash { get; set; } = string.Empty;
    }

    /// <summary>
    /// Detected suspicious activities, policy violations, spam, bot behavior, and threat risk scoring.
    /// </summary>
    public class AgentSecurityEvent
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string EventCode { get; set; } = string.Empty; // e.g. "SEC-202611-0042"

        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [MaxLength(150)]
        public string? UserEmail { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [Required, MaxLength(100)]
        public string ViolationCategory { get; set; } = "SuspiciousActivity"; // "Spam", "CredentialAbuse", "MaliciousPayload", "BotActivity", "PolicyViolation", "DonationTampering"

        [Required, MaxLength(500)]
        public string DetectionDetails { get; set; } = string.Empty;

        public double RiskScore { get; set; } = 0.5; // 0.0 to 1.0

        [Required, MaxLength(20)]
        public string Severity { get; set; } = "Medium"; // "Low", "Medium", "High", "Critical"

        public bool WarningIssued { get; set; } = false;
        public bool RestrictionTriggered { get; set; } = false;

        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string DetectionEngine { get; set; } = "Deterministic+AI";
    }

    /// <summary>
    /// Reversible temporary account restrictions resulting from verified policy violations.
    /// </summary>
    public class AgentUserRestriction
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required, MaxLength(100)]
        public string ViolationCategory { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;

        public DateTime RestrictedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; } // null = until reviewed

        public bool IsActive { get; set; } = true;
        public bool CountdownCompleted { get; set; } = true; // 3-second alert confirmation

        public bool SuspensionEmailSent { get; set; } = false;

        public string? RevokedSessionId { get; set; }

        // Reverse / Restoration info
        public DateTime? RestoredAt { get; set; }
        public string? RestoredBy { get; set; }
        public string? RestorationReason { get; set; }
    }

    /// <summary>
    /// User appeal submission for restricted/suspended accounts.
    /// </summary>
    public class AgentAppeal
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        public int RestrictionId { get; set; }
        [ForeignKey("RestrictionId")]
        public virtual AgentUserRestriction? Restriction { get; set; }

        [Required, MaxLength(2000)]
        public string Explanation { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? ContactEmail { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(30)]
        public string Status { get; set; } = "Pending"; // "Pending", "UnderReview", "Approved", "Rejected"

        public string? AdminNotes { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewedBy { get; set; }
    }

    /// <summary>
    /// Isolated conversation thread between a user/admin and the designated AI agent.
    /// </summary>
    public class AgentConversation
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string SessionId { get; set; } = Guid.NewGuid().ToString("N");

        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required, MaxLength(30)]
        public string AgentType { get; set; } = "UserAgent"; // "UserAgent" or "AdminAgent"

        [Required, MaxLength(20)]
        public string Language { get; set; } = "en"; // "en", "ur", "ur-PK" (Roman Urdu)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<AgentMessage> Messages { get; set; } = new List<AgentMessage>();
    }

    /// <summary>
    /// Individual message in an agent conversation.
    /// </summary>
    public class AgentMessage
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }
        [ForeignKey("ConversationId")]
        public virtual AgentConversation? Conversation { get; set; }

        [Required, MaxLength(20)]
        public string SenderRole { get; set; } = "User"; // "User", "Agent", "System"

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? ProviderUsed { get; set; }

        public double? ProcessingTimeSec { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Scheduled autonomous automation execution log.
    /// </summary>
    public class AgentAutomationJob
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string JobName { get; set; } = string.Empty; // e.g. "DonationMonitoringJob", "SecurityScannerJob"

        [Required, MaxLength(100)]
        public string ExecutionId { get; set; } = Guid.NewGuid().ToString("N");

        [Required, MaxLength(30)]
        public string Status { get; set; } = "Running"; // "Running", "Completed", "Failed", "Skipped"

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        public int RecordsEvaluated { get; set; } = 0;
        public int ActionsTriggered { get; set; } = 0;

        public string? SummaryMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public int RetryCount { get; set; } = 0;
    }

    /// <summary>
    /// AI Provider tokens, latency, and operational health metric log.
    /// </summary>
    public class AgentUsageStatistic
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Provider { get; set; } = "Groq"; // "Groq", "Gemini"

        [Required, MaxLength(100)]
        public string ModelName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string OperationType { get; set; } = "Chat"; // "Chat", "IntelligenceAnalysis", "SecurityClassification", "ReportGeneration"

        public int PromptTokens { get; set; } = 0;
        public int CompletionTokens { get; set; } = 0;
        public int TotalTokens { get; set; } = 0;

        public double LatencyMs { get; set; } = 0.0;
        public bool IsSuccessful { get; set; } = true;
        public string? ErrorCategory { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
