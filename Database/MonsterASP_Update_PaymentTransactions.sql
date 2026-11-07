-- ============================================================================
-- NGEOS - Production Migration Script for MonsterASP MS SQL Database
-- Use this script in MonsterASP Control Panel -> Databases -> MS SQL -> Web Query
-- to add the new Multi-Gateway PaymentTransactions table safely without data loss.
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'PaymentTransactions')
BEGIN
    CREATE TABLE [PaymentTransactions] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [DonationId] INT NOT NULL,
        [GatewayName] NVARCHAR(50) NOT NULL,
        [InternalReference] NVARCHAR(100) NOT NULL,
        [GatewayReference] NVARCHAR(150) NULL,
        [Amount] DECIMAL(18,2) NOT NULL,
        [Currency] NVARCHAR(10) NOT NULL,
        [Status] NVARCHAR(50) NOT NULL,
        [Environment] NVARCHAR(20) NOT NULL,
        [VerificationStatus] NVARCHAR(50) NOT NULL,
        [IdempotencyKey] NVARCHAR(100) NULL,
        [RawCallbackPayload] NVARCHAR(MAX) NULL,
        [RawWebhookPayload] NVARCHAR(MAX) NULL,
        [ErrorCode] NVARCHAR(50) NULL,
        [ErrorMessage] NVARCHAR(MAX) NULL,
        [RefundAmount] DECIMAL(18,2) NULL,
        [RefundReference] NVARCHAR(150) NULL,
        [RefundReason] NVARCHAR(250) NULL,
        [RefundedAt] DATETIME2 NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [CompletedAt] DATETIME2 NULL,
        CONSTRAINT [PK_PaymentTransactions] PRIMARY KEY ([Id])
    );
    PRINT 'PaymentTransactions table created successfully.';
END
ELSE
BEGIN
    PRINT 'PaymentTransactions table already exists.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PaymentTransactions_Donations')
BEGIN
    ALTER TABLE [PaymentTransactions] ADD CONSTRAINT [FK_PaymentTransactions_Donations] FOREIGN KEY ([DonationId]) REFERENCES [Donations] ([Id]) ON DELETE CASCADE;
    PRINT 'FK_PaymentTransactions_Donations created.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentTransactions_DonationId' AND object_id = OBJECT_ID(N'PaymentTransactions'))
BEGIN
    CREATE INDEX [IX_PaymentTransactions_DonationId] ON [PaymentTransactions] ([DonationId]);
    PRINT 'IX_PaymentTransactions_DonationId created.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentTransactions_GatewayName_GatewayReference' AND object_id = OBJECT_ID(N'PaymentTransactions'))
BEGIN
    CREATE INDEX [IX_PaymentTransactions_GatewayName_GatewayReference] ON [PaymentTransactions] ([GatewayName], [GatewayReference]);
    PRINT 'IX_PaymentTransactions_GatewayName_GatewayReference created.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentTransactions_InternalReference' AND object_id = OBJECT_ID(N'PaymentTransactions'))
BEGIN
    CREATE UNIQUE INDEX [IX_PaymentTransactions_InternalReference] ON [PaymentTransactions] ([InternalReference]);
    PRINT 'IX_PaymentTransactions_InternalReference created.';
END
GO
