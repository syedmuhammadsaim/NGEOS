using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NGEOS.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentEcosystemTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentAutomationJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExecutionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecordsEvaluated = table.Column<int>(type: "int", nullable: false),
                    ActionsTriggered = table.Column<int>(type: "int", nullable: false),
                    SummaryMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentAutomationJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsAdminAgentEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsUserAgentEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsAutomationPaused = table.Column<bool>(type: "bit", nullable: false),
                    IsEmergencyKillSwitchActive = table.Column<bool>(type: "bit", nullable: false),
                    PrimaryProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FallbackProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GroqModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GeminiModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DonationMonitoringIntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    SecurityScanIntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    HealthCheckIntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    LastSuccessfulExecution = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentConversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    AgentType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActiveAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentConversations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AgentReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AgentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AgentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetUserId = table.Column<int>(type: "int", nullable: true),
                    AffectedRecordId = table.Column<int>(type: "int", nullable: true),
                    ActionDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ActionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PreviousValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RiskLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ActionStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExecutionDurationMs = table.Column<double>(type: "float", nullable: false),
                    RelatedSecurityEventId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ErrorDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IntegrityHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentReports_Users_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AgentSecurityEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    UserEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ViolationCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DetectionDetails = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RiskScore = table.Column<double>(type: "float", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    WarningIssued = table.Column<bool>(type: "bit", nullable: false),
                    RestrictionTriggered = table.Column<bool>(type: "bit", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DetectionEngine = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentSecurityEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentSecurityEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AgentUsageStatistics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PromptTokens = table.Column<int>(type: "int", nullable: false),
                    CompletionTokens = table.Column<int>(type: "int", nullable: false),
                    TotalTokens = table.Column<int>(type: "int", nullable: false),
                    LatencyMs = table.Column<double>(type: "float", nullable: false),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    ErrorCategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentUsageStatistics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentUserRestrictions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ViolationCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RestrictedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CountdownCompleted = table.Column<bool>(type: "bit", nullable: false),
                    SuspensionEmailSent = table.Column<bool>(type: "bit", nullable: false),
                    RevokedSessionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RestoredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RestoredBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RestorationReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentUserRestrictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentUserRestrictions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonationId = table.Column<int>(type: "int", nullable: false),
                    GatewayName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InternalReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GatewayReference = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VerificationStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    RawCallbackPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawWebhookPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RefundReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RefundReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Donations_DonationId",
                        column: x => x.DonationId,
                        principalTable: "Donations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<int>(type: "int", nullable: false),
                    SenderRole = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProviderUsed = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProcessingTimeSec = table.Column<double>(type: "float", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentMessages_AgentConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "AgentConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentAppeals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RestrictionId = table.Column<int>(type: "int", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AdminNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentAppeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentAppeals_AgentUserRestrictions_RestrictionId",
                        column: x => x.RestrictionId,
                        principalTable: "AgentUserRestrictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentAppeals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Donations_ProgrammeId",
                table: "Donations",
                column: "ProgrammeId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentAppeals_RestrictionId",
                table: "AgentAppeals",
                column: "RestrictionId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentAppeals_UserId",
                table: "AgentAppeals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentAutomationJobs_ExecutionId",
                table: "AgentAutomationJobs",
                column: "ExecutionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentConversations_SessionId",
                table: "AgentConversations",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentConversations_UserId",
                table: "AgentConversations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentMessages_ConversationId",
                table: "AgentMessages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentReports_ReportCode",
                table: "AgentReports",
                column: "ReportCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentReports_TargetUserId",
                table: "AgentReports",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentReports_Timestamp",
                table: "AgentReports",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AgentSecurityEvents_DetectedAt",
                table: "AgentSecurityEvents",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AgentSecurityEvents_EventCode",
                table: "AgentSecurityEvents",
                column: "EventCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentSecurityEvents_UserId",
                table: "AgentSecurityEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentUserRestrictions_UserId",
                table: "AgentUserRestrictions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_DonationId",
                table: "PaymentTransactions",
                column: "DonationId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_GatewayName_GatewayReference",
                table: "PaymentTransactions",
                columns: new[] { "GatewayName", "GatewayReference" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_InternalReference",
                table: "PaymentTransactions",
                column: "InternalReference",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Donations_Programmes_ProgrammeId",
                table: "Donations",
                column: "ProgrammeId",
                principalTable: "Programmes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Donations_Programmes_ProgrammeId",
                table: "Donations");

            migrationBuilder.DropTable(
                name: "AgentAppeals");

            migrationBuilder.DropTable(
                name: "AgentAutomationJobs");

            migrationBuilder.DropTable(
                name: "AgentConfigurations");

            migrationBuilder.DropTable(
                name: "AgentMessages");

            migrationBuilder.DropTable(
                name: "AgentReports");

            migrationBuilder.DropTable(
                name: "AgentSecurityEvents");

            migrationBuilder.DropTable(
                name: "AgentUsageStatistics");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "AgentUserRestrictions");

            migrationBuilder.DropTable(
                name: "AgentConversations");

            migrationBuilder.DropIndex(
                name: "IX_Donations_ProgrammeId",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "PaymentGateway",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "ProgrammeId",
                table: "Donations");
        }
    }
}
