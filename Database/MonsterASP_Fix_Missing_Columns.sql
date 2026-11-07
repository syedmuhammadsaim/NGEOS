-- ============================================================================
-- NGEOS - Quick Database Fix for Missing Columns on MonsterASP
-- Run this in MonsterASP -> Databases -> MS SQL -> Run T-SQL
-- ============================================================================

-- 1. Add missing columns to Donations table
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Donations') AND name = N'PaymentGateway')
BEGIN
    ALTER TABLE [Donations] ADD [PaymentGateway] NVARCHAR(50) NOT NULL CONSTRAINT [DF_Donations_PaymentGateway] DEFAULT N'Card';
    PRINT 'Added PaymentGateway column to Donations.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Donations') AND name = N'Currency')
BEGIN
    ALTER TABLE [Donations] ADD [Currency] NVARCHAR(10) NOT NULL CONSTRAINT [DF_Donations_Currency] DEFAULT N'PKR';
    PRINT 'Added Currency column to Donations.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Donations') AND name = N'ProgrammeId')
BEGIN
    ALTER TABLE [Donations] ADD [ProgrammeId] INT NULL;
    PRINT 'Added ProgrammeId column to Donations.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Donations_Programmes_ProgrammeId')
BEGIN
    ALTER TABLE [Donations] ADD CONSTRAINT [FK_Donations_Programmes_ProgrammeId] FOREIGN KEY ([ProgrammeId]) REFERENCES [Programmes] ([Id]);
    PRINT 'Created FK_Donations_Programmes_ProgrammeId.';
END
GO

-- 2. Add missing columns to Users table
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Users') AND name = N'GoogleId')
BEGIN
    ALTER TABLE [Users] ADD [GoogleId] NVARCHAR(100) NULL;
    PRINT 'Added GoogleId column to Users.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_GoogleId' AND object_id = OBJECT_ID(N'Users'))
BEGIN
    CREATE INDEX [IX_Users_GoogleId] ON [Users] ([GoogleId]);
    PRINT 'Created index IX_Users_GoogleId.';
END
GO

PRINT 'Database columns check and update completed successfully!';
