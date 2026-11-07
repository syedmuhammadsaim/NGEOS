-- ============================================================================
-- NGEOS - Official Complete SQL Server Database Script
-- Generated: 2026-11-08 03:29:11
-- Includes all 19 tables, indexes, foreign keys, and complete seed data
-- (Including new PaymentTransactions multi-gateway architecture & AI tables)
-- ============================================================================

USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'NGEOSDb')
BEGIN
    CREATE DATABASE [NGEOSDb];
END
GO

USE [NGEOSDb];
GO

-- Table: [__EFMigrationsHistory]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'__EFMigrationsHistory')
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] NVARCHAR(150) NOT NULL,
        [ProductVersion] NVARCHAR(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END
GO

-- Table: [Users]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Users')
BEGIN
    CREATE TABLE [Users] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [FullName] NVARCHAR(100) NOT NULL,
        [Email] NVARCHAR(150) NOT NULL,
        [PasswordHash] NVARCHAR(MAX) NOT NULL,
        [Phone] NVARCHAR(25) NULL,
        [Address] NVARCHAR(250) NULL,
        [Profession] NVARCHAR(100) NULL,
        [Organization] NVARCHAR(150) NULL,
        [Role] NVARCHAR(20) NOT NULL,
        [ProfileImageUrl] NVARCHAR(250) NULL,
        [IsActive] BIT NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [GoogleId] NVARCHAR(100) NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END
GO

-- Table: [AboutPages]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AboutPages')
BEGIN
    CREATE TABLE [AboutPages] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Slug] NVARCHAR(60) NOT NULL,
        [Title] NVARCHAR(150) NOT NULL,
        [Subtitle] NVARCHAR(250) NULL,
        [ContentHtml] NVARCHAR(MAX) NOT NULL,
        [BannerImageUrl] NVARCHAR(250) NULL,
        [FeaturedImageUrl] NVARCHAR(250) NULL,
        [LastUpdated] DATETIME2 NOT NULL,
        CONSTRAINT [PK_AboutPages] PRIMARY KEY ([Id])
    );
END
GO

-- Table: [NGOs]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'NGOs')
BEGIN
    CREATE TABLE [NGOs] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(150) NOT NULL,
        [RegistrationNumber] NVARCHAR(50) NOT NULL,
        [ShortDescription] NVARCHAR(300) NOT NULL,
        [FullDescription] NVARCHAR(MAX) NOT NULL,
        [LogoUrl] NVARCHAR(250) NOT NULL,
        [BannerUrl] NVARCHAR(250) NULL,
        [FocusArea] NVARCHAR(100) NOT NULL,
        [Website] NVARCHAR(200) NULL,
        [Email] NVARCHAR(150) NOT NULL,
        [Phone] NVARCHAR(30) NOT NULL,
        [Address] NVARCHAR(250) NULL,
        [EstablishedYear] INT NOT NULL,
        [IsVerified] BIT NOT NULL,
        [IsActive] BIT NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_NGOs] PRIMARY KEY ([Id])
    );
END
GO

-- Table: [Causes]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Causes')
BEGIN
    CREATE TABLE [Causes] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Title] NVARCHAR(150) NOT NULL,
        [ShortDescription] NVARCHAR(350) NOT NULL,
        [FullDescription] NVARCHAR(MAX) NOT NULL,
        [Category] NVARCHAR(50) NOT NULL,
        [TargetAmount] DECIMAL(18, 2) NOT NULL,
        [RaisedAmount] DECIMAL(18, 2) NOT NULL,
        [ImageUrl] NVARCHAR(300) NOT NULL,
        [IsFeatured] BIT NOT NULL,
        [IsActive] BIT NOT NULL,
        [NgoId] INT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_Causes] PRIMARY KEY ([Id])
    );
END
GO

-- Table: [Programmes]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Programmes')
BEGIN
    CREATE TABLE [Programmes] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Title] NVARCHAR(150) NOT NULL,
        [NgoId] INT NULL,
        [Category] NVARCHAR(100) NOT NULL,
        [ShortDescription] NVARCHAR(350) NOT NULL,
        [FullDescription] NVARCHAR(MAX) NOT NULL,
        [ImageUrl] NVARCHAR(250) NOT NULL,
        [StartDate] DATETIME2 NOT NULL,
        [EndDate] DATETIME2 NULL,
        [Location] NVARCHAR(150) NOT NULL,
        [TargetBeneficiaries] INT NOT NULL,
        [IsActive] BIT NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_Programmes] PRIMARY KEY ([Id])
    );
END
GO

-- Table: [Events]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Events')
BEGIN
    CREATE TABLE [Events] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Title] NVARCHAR(150) NOT NULL,
        [ShortDescription] NVARCHAR(350) NOT NULL,
        [FullDescription] NVARCHAR(MAX) NOT NULL,
        [EventDate] DATETIME2 NOT NULL,
        [Location] NVARCHAR(150) NOT NULL,
        [ImageUrl] NVARCHAR(250) NOT NULL,
        [Organizer] NVARCHAR(100) NOT NULL,
        [IsUpcoming] BIT NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_Events] PRIMARY KEY ([Id])
    );
END
GO

-- Table: [GalleryItems]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'GalleryItems')
BEGIN
    CREATE TABLE [GalleryItems] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Title] NVARCHAR(150) NOT NULL,
        [Category] NVARCHAR(50) NOT NULL,
        [ImageUrl] NVARCHAR(250) NOT NULL,
        [Description] NVARCHAR(350) NULL,
        [EventDate] DATETIME2 NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_GalleryItems] PRIMARY KEY ([Id])
    );
END
GO

-- Table: [JobOpportunities]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'JobOpportunities')
BEGIN
    CREATE TABLE [JobOpportunities] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Title] NVARCHAR(150) NOT NULL,
        [Department] NVARCHAR(100) NOT NULL,
        [JobType] NVARCHAR(50) NOT NULL,
        [Location] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(MAX) NOT NULL,
        [Requirements] NVARCHAR(MAX) NOT NULL,
        [IsActive] BIT NOT NULL,
        [PostedDate] DATETIME2 NOT NULL,
        CONSTRAINT [PK_JobOpportunities] PRIMARY KEY ([Id])
    );
END
GO

-- Table: [Partners]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Partners')
BEGIN
    CREATE TABLE [Partners] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(150) NOT NULL,
        [Category] NVARCHAR(100) NOT NULL,
        [LogoUrl] NVARCHAR(250) NOT NULL,
        [WebsiteUrl] NVARCHAR(200) NULL,
        [Description] NVARCHAR(350) NULL,
        [DisplayOrder] INT NOT NULL,
        [IsActive] BIT NOT NULL,
        CONSTRAINT [PK_Partners] PRIMARY KEY ([Id])
    );
END
GO

-- Table: [TeamMembers]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'TeamMembers')
BEGIN
    CREATE TABLE [TeamMembers] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [Role] NVARCHAR(100) NOT NULL,
        [Bio] NVARCHAR(350) NOT NULL,
        [PhotoUrl] NVARCHAR(250) NOT NULL,
        [Email] NVARCHAR(200) NULL,
        [LinkedInUrl] NVARCHAR(200) NULL,
        [TwitterUrl] NVARCHAR(200) NULL,
        [DisplayOrder] INT NOT NULL,
        [IsActive] BIT NOT NULL,
        CONSTRAINT [PK_TeamMembers] PRIMARY KEY ([Id])
    );
END
GO

-- Table: [FAQs]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'FAQs')
BEGIN
    CREATE TABLE [FAQs] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Question] NVARCHAR(250) NOT NULL,
        [Answer] NVARCHAR(MAX) NOT NULL,
        [Category] NVARCHAR(100) NOT NULL,
        [DisplayOrder] INT NOT NULL,
        [IsActive] BIT NOT NULL,
        CONSTRAINT [PK_FAQs] PRIMARY KEY ([Id])
    );
END
GO

-- Table: [ContactMessages]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ContactMessages')
BEGIN
    CREATE TABLE [ContactMessages] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [Email] NVARCHAR(150) NOT NULL,
        [Phone] NVARCHAR(25) NULL,
        [Subject] NVARCHAR(200) NOT NULL,
        [Message] NVARCHAR(MAX) NOT NULL,
        [IsRead] BIT NOT NULL,
        [ReplyNotes] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_ContactMessages] PRIMARY KEY ([Id])
    );
END
GO

-- Table: [UserQueries]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'UserQueries')
BEGIN
    CREATE TABLE [UserQueries] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NULL,
        [FullName] NVARCHAR(100) NOT NULL,
        [Email] NVARCHAR(150) NOT NULL,
        [Subject] NVARCHAR(200) NOT NULL,
        [Category] NVARCHAR(100) NOT NULL,
        [Message] NVARCHAR(MAX) NOT NULL,
        [AdminReply] NVARCHAR(MAX) NULL,
        [Status] NVARCHAR(30) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [RepliedAt] DATETIME2 NULL,
        CONSTRAINT [PK_UserQueries] PRIMARY KEY ([Id])
    );
END
GO

-- Table: [Donations]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Donations')
BEGIN
    CREATE TABLE [Donations] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [TransactionId] NVARCHAR(50) NOT NULL,
        [CauseId] INT NOT NULL,
        [UserId] INT NULL,
        [DonorName] NVARCHAR(100) NOT NULL,
        [DonorEmail] NVARCHAR(150) NOT NULL,
        [DonorPhone] NVARCHAR(25) NULL,
        [Amount] DECIMAL(18, 2) NOT NULL,
        [CardType] NVARCHAR(30) NOT NULL,
        [CardHolderName] NVARCHAR(100) NOT NULL,
        [MaskedCardNumber] NVARCHAR(30) NOT NULL,
        [PaymentStatus] NVARCHAR(30) NOT NULL,
        [Notes] NVARCHAR(500) NULL,
        [DonationDate] DATETIME2 NOT NULL,
        [ProgrammeId] INT NULL,
        [PaymentGateway] NVARCHAR(50) NOT NULL DEFAULT ('Credit Card'),
        [Currency] NVARCHAR(10) NOT NULL DEFAULT ('PKR'),
        CONSTRAINT [PK_Donations] PRIMARY KEY ([Id])
    );
END
GO

-- Table: [PaymentTransactions]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'PaymentTransactions')
BEGIN
    CREATE TABLE [PaymentTransactions] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [DonationId] INT NOT NULL,
        [GatewayName] NVARCHAR(50) NOT NULL,
        [InternalReference] NVARCHAR(100) NOT NULL,
        [GatewayReference] NVARCHAR(150) NULL,
        [Amount] DECIMAL(18, 2) NOT NULL,
        [Currency] NVARCHAR(10) NOT NULL,
        [Status] NVARCHAR(30) NOT NULL,
        [Environment] NVARCHAR(20) NOT NULL,
        [VerificationStatus] NVARCHAR(30) NOT NULL,
        [IdempotencyKey] NVARCHAR(150) NULL,
        [RawCallbackPayload] NVARCHAR(MAX) NULL,
        [RawWebhookPayload] NVARCHAR(MAX) NULL,
        [ErrorCode] NVARCHAR(50) NULL,
        [ErrorMessage] NVARCHAR(500) NULL,
        [RefundAmount] DECIMAL(18, 2) NULL,
        [RefundReference] NVARCHAR(100) NULL,
        [RefundReason] NVARCHAR(300) NULL,
        [RefundedAt] DATETIME2 NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [CompletedAt] DATETIME2 NULL,
        CONSTRAINT [PK__PaymentT__3214EC078DFC59E0] PRIMARY KEY ([Id])
    );
END
GO

-- Table: [ProgrammeInterests]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ProgrammeInterests')
BEGIN
    CREATE TABLE [ProgrammeInterests] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ProgrammeId] INT NOT NULL,
        [UserId] INT NULL,
        [FullName] NVARCHAR(100) NOT NULL,
        [Email] NVARCHAR(150) NOT NULL,
        [Phone] NVARCHAR(25) NOT NULL,
        [Occupation] NVARCHAR(100) NULL,
        [InterestType] NVARCHAR(50) NOT NULL,
        [Message] NVARCHAR(500) NULL,
        [Status] NVARCHAR(30) NOT NULL,
        [SubmittedAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_ProgrammeInterests] PRIMARY KEY ([Id])
    );
END
GO

-- Table: [FriendInvitations]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'FriendInvitations')
BEGIN
    CREATE TABLE [FriendInvitations] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NULL,
        [SenderName] NVARCHAR(100) NOT NULL,
        [SenderEmail] NVARCHAR(150) NOT NULL,
        [FriendEmail] NVARCHAR(150) NOT NULL,
        [CauseId] INT NULL,
        [PersonalMessage] NVARCHAR(600) NULL,
        [SentAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_FriendInvitations] PRIMARY KEY ([Id])
    );
END
GO

-- Table: [PasswordResetTokens]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'PasswordResetTokens')
BEGIN
    CREATE TABLE [PasswordResetTokens] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [Code] NVARCHAR(6) NOT NULL,
        [ResetSessionToken] NVARCHAR(100) NULL,
        [IsVerified] BIT NOT NULL,
        [Expiration] DATETIME2 NOT NULL,
        [IsUsed] BIT NOT NULL,
        [Attempts] INT NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([Id])
    );
END
GO

-- ============================================================
-- Foreign Key Constraints
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Causes_NGOs_NgoId')
BEGIN
    ALTER TABLE [Causes] ADD CONSTRAINT [FK_Causes_NGOs_NgoId] FOREIGN KEY ([NgoId]) REFERENCES [NGOs] ([Id]) ON DELETE SET NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Donations_Causes_CauseId')
BEGIN
    ALTER TABLE [Donations] ADD CONSTRAINT [FK_Donations_Causes_CauseId] FOREIGN KEY ([CauseId]) REFERENCES [Causes] ([Id]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Donations_Programmes')
BEGIN
    ALTER TABLE [Donations] ADD CONSTRAINT [FK_Donations_Programmes] FOREIGN KEY ([ProgrammeId]) REFERENCES [Programmes] ([Id]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Donations_Users_UserId')
BEGIN
    ALTER TABLE [Donations] ADD CONSTRAINT [FK_Donations_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FriendInvitations_Causes_CauseId')
BEGIN
    ALTER TABLE [FriendInvitations] ADD CONSTRAINT [FK_FriendInvitations_Causes_CauseId] FOREIGN KEY ([CauseId]) REFERENCES [Causes] ([Id]) ON DELETE SET NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FriendInvitations_Users_UserId')
BEGIN
    ALTER TABLE [FriendInvitations] ADD CONSTRAINT [FK_FriendInvitations_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PasswordResetTokens_Users_UserId')
BEGIN
    ALTER TABLE [PasswordResetTokens] ADD CONSTRAINT [FK_PasswordResetTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PaymentTransactions_Donations')
BEGIN
    ALTER TABLE [PaymentTransactions] ADD CONSTRAINT [FK_PaymentTransactions_Donations] FOREIGN KEY ([DonationId]) REFERENCES [Donations] ([Id]) ON DELETE CASCADE;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProgrammeInterests_Programmes_ProgrammeId')
BEGIN
    ALTER TABLE [ProgrammeInterests] ADD CONSTRAINT [FK_ProgrammeInterests_Programmes_ProgrammeId] FOREIGN KEY ([ProgrammeId]) REFERENCES [Programmes] ([Id]) ON DELETE CASCADE;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProgrammeInterests_Users_UserId')
BEGIN
    ALTER TABLE [ProgrammeInterests] ADD CONSTRAINT [FK_ProgrammeInterests_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Programmes_NGOs_NgoId')
BEGIN
    ALTER TABLE [Programmes] ADD CONSTRAINT [FK_Programmes_NGOs_NgoId] FOREIGN KEY ([NgoId]) REFERENCES [NGOs] ([Id]) ON DELETE SET NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UserQueries_Users_UserId')
BEGIN
    ALTER TABLE [UserQueries] ADD CONSTRAINT [FK_UserQueries_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL;
END
GO

-- ============================================================
-- Indexes
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AboutPages_Slug' AND object_id = OBJECT_ID(N'AboutPages'))
BEGIN
    CREATE UNIQUE INDEX [IX_AboutPages_Slug] ON [AboutPages] ([Slug]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Causes_NgoId' AND object_id = OBJECT_ID(N'Causes'))
BEGIN
    CREATE INDEX [IX_Causes_NgoId] ON [Causes] ([NgoId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Donations_CauseId' AND object_id = OBJECT_ID(N'Donations'))
BEGIN
    CREATE INDEX [IX_Donations_CauseId] ON [Donations] ([CauseId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Donations_UserId' AND object_id = OBJECT_ID(N'Donations'))
BEGIN
    CREATE INDEX [IX_Donations_UserId] ON [Donations] ([UserId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FriendInvitations_CauseId' AND object_id = OBJECT_ID(N'FriendInvitations'))
BEGIN
    CREATE INDEX [IX_FriendInvitations_CauseId] ON [FriendInvitations] ([CauseId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FriendInvitations_UserId' AND object_id = OBJECT_ID(N'FriendInvitations'))
BEGIN
    CREATE INDEX [IX_FriendInvitations_UserId] ON [FriendInvitations] ([UserId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PasswordResetTokens_Code' AND object_id = OBJECT_ID(N'PasswordResetTokens'))
BEGIN
    CREATE INDEX [IX_PasswordResetTokens_Code] ON [PasswordResetTokens] ([Code]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PasswordResetTokens_ResetSessionToken' AND object_id = OBJECT_ID(N'PasswordResetTokens'))
BEGIN
    CREATE INDEX [IX_PasswordResetTokens_ResetSessionToken] ON [PasswordResetTokens] ([ResetSessionToken]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PasswordResetTokens_UserId' AND object_id = OBJECT_ID(N'PasswordResetTokens'))
BEGIN
    CREATE INDEX [IX_PasswordResetTokens_UserId] ON [PasswordResetTokens] ([UserId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentTransactions_GatewayName_GatewayReference' AND object_id = OBJECT_ID(N'PaymentTransactions'))
BEGIN
    CREATE INDEX [IX_PaymentTransactions_GatewayName_GatewayReference] ON [PaymentTransactions] ([GatewayName], [GatewayReference]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentTransactions_InternalReference' AND object_id = OBJECT_ID(N'PaymentTransactions'))
BEGIN
    CREATE UNIQUE INDEX [IX_PaymentTransactions_InternalReference] ON [PaymentTransactions] ([InternalReference]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProgrammeInterests_ProgrammeId' AND object_id = OBJECT_ID(N'ProgrammeInterests'))
BEGIN
    CREATE INDEX [IX_ProgrammeInterests_ProgrammeId] ON [ProgrammeInterests] ([ProgrammeId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProgrammeInterests_UserId' AND object_id = OBJECT_ID(N'ProgrammeInterests'))
BEGIN
    CREATE INDEX [IX_ProgrammeInterests_UserId] ON [ProgrammeInterests] ([UserId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Programmes_NgoId' AND object_id = OBJECT_ID(N'Programmes'))
BEGIN
    CREATE INDEX [IX_Programmes_NgoId] ON [Programmes] ([NgoId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserQueries_UserId' AND object_id = OBJECT_ID(N'UserQueries'))
BEGIN
    CREATE INDEX [IX_UserQueries_UserId] ON [UserQueries] ([UserId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_Email' AND object_id = OBJECT_ID(N'Users'))
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_GoogleId' AND object_id = OBJECT_ID(N'Users'))
BEGIN
    CREATE INDEX [IX_Users_GoogleId] ON [Users] ([GoogleId]);
END
GO

-- ============================================================
-- Complete Verified Seed Data & Records
-- ============================================================

-- Seed Data: [__EFMigrationsHistory] (3 rows)
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260827080324_InitialCreate')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260827080324_InitialCreate', N'10.0.11');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261025091132_AddPasswordResetTokens')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261025091132_AddPasswordResetTokens', N'10.0.11');
GO
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261025102850_AddGoogleIdToUser')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261025102850_AddGoogleIdToUser', N'10.0.11');
GO
-- Seed Data: [Users] (9 rows)
SET IDENTITY_INSERT [Users] ON;
GO
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Id] = 1)
    INSERT INTO [Users] ([Id], [FullName], [Email], [PasswordHash], [Phone], [Address], [Profession], [Organization], [Role], [ProfileImageUrl], [IsActive], [CreatedAt], [GoogleId]) VALUES (1, N'Give-AID Administrator', N'admin@giveaid.org', N'18749713f33bf217f1bc9b30fabb4f3454bcc736bd10ec6d02f872a1cd7034b6', N' +92 (300) 123-4567', N' 100 Edhi Street, Saddar, Karachi, Pakistan', N'NGO Managing Director', N'Give-AID Central Secretariat', N'User', N'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=300&auto=format&fit=crop&q=80', 1, N'2026-10-24 18:16:31.746', NULL);
GO
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Id] = 2)
    INSERT INTO [Users] ([Id], [FullName], [Email], [PasswordHash], [Phone], [Address], [Profession], [Organization], [Role], [ProfileImageUrl], [IsActive], [CreatedAt], [GoogleId]) VALUES (2, N'NGEOS Administrator', N'syedmuhammadsaim.contact@gmail.com', N'246aba5deef8b6e7c474ee2bc8491f08fcc8de12073b8d9f97249f805d056307', N'33378345632', N'karachi', N'Software', N'Atech', N'User', N'/uploads/profiles/user_2_87b39c008e214745b7b5d15cfe738379.jpg', 1, N'2026-10-24 19:08:38.023', N'103699666188542104980');
GO
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Id] = 3)
    INSERT INTO [Users] ([Id], [FullName], [Email], [PasswordHash], [Phone], [Address], [Profession], [Organization], [Role], [ProfileImageUrl], [IsActive], [CreatedAt], [GoogleId]) VALUES (3, N'SYED MUHAMMAD SAIM', N'syedmuhammadsaim.busniess@gmail.com', N'd6edcf55e855a4783116dc904e695e3f4a36ce26497fdb11d1279b28de4bb0c6', N'33378345632', N'azambasti, karachi', N'Software', N'GIVE-AID', N'User', NULL, 1, N'2026-10-25 09:34:32.638', NULL);
GO
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Id] = 4)
    INSERT INTO [Users] ([Id], [FullName], [Email], [PasswordHash], [Phone], [Address], [Profession], [Organization], [Role], [ProfileImageUrl], [IsActive], [CreatedAt], [GoogleId]) VALUES (4, N'SYED MUHAMMAD SAIM', N'syedmuhammadsaim.business@gmail.com', N'73e055fb0b58ce5361e5e1671d2c7fda1985c5446c825a6e7788276f23a6b242', N'+92 1212121212', N'azambasti, karachi', N'Software', N'GIVE-AID', N'User', N'/uploads/profiles/user_4_bc485d8424954b0ea1f9553bc0e2f283.jpg', 1, N'2026-10-27 15:00:18.422', NULL);
GO
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Id] = 5)
    INSERT INTO [Users] ([Id], [FullName], [Email], [PasswordHash], [Phone], [Address], [Profession], [Organization], [Role], [ProfileImageUrl], [IsActive], [CreatedAt], [GoogleId]) VALUES (5, N'Abdullah Bukhari', N'abdull4h5599@gmail.com', N'5a3772de899d6a64f3323efd401459d407ece58eba82818b4d5fe2f4c253c407', NULL, NULL, N'Software', N'Atech', N'User', N'/uploads/profiles/user_5_a253aebd932447af8f577e156ac21d4d.jpg', 1, N'2026-10-27 18:45:54.247', N'103784599191402378377');
GO
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Id] = 6)
    INSERT INTO [Users] ([Id], [FullName], [Email], [PasswordHash], [Phone], [Address], [Profession], [Organization], [Role], [ProfileImageUrl], [IsActive], [CreatedAt], [GoogleId]) VALUES (6, N'SYED MUHAMMAD SAIM', N'admin@cinevora.local', N'2ac1b5e3749130ac4c751d888e374090ea7af1786d21716e0e9419b6f7dabce8', N'+923378034564', N'azambasti, karachi', N'Software', N'GIVE-AID', N'User', NULL, 1, N'2026-11-01 19:29:40.737', NULL);
GO
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Id] = 7)
    INSERT INTO [Users] ([Id], [FullName], [Email], [PasswordHash], [Phone], [Address], [Profession], [Organization], [Role], [ProfileImageUrl], [IsActive], [CreatedAt], [GoogleId]) VALUES (7, N'NGEOS Administrator', N'ngeos@gmail.com', N'18749713f33bf217f1bc9b30fabb4f3454bcc736bd10ec6d02f872a1cd7034b6', NULL, NULL, NULL, NULL, N'Admin', NULL, 1, N'2026-11-03 06:45:26.370', NULL);
GO
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Id] = 8)
    INSERT INTO [Users] ([Id], [FullName], [Email], [PasswordHash], [Phone], [Address], [Profession], [Organization], [Role], [ProfileImageUrl], [IsActive], [CreatedAt], [GoogleId]) VALUES (8, N'saim', N'saim1@gmail.com', N'd6edcf55e855a4783116dc904e695e3f4a36ce26497fdb11d1279b28de4bb0c6', N'+92 2222222222', N'karachi', N'Software', N'GIVE-AID', N'User', NULL, 1, N'2026-11-05 15:28:39.921', NULL);
GO
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Id] = 9)
    INSERT INTO [Users] ([Id], [FullName], [Email], [PasswordHash], [Phone], [Address], [Profession], [Organization], [Role], [ProfileImageUrl], [IsActive], [CreatedAt], [GoogleId]) VALUES (9, N'NGEOS Administrator', N'admin@ngeos.org', N'18749713f33bf217f1bc9b30fabb4f3454bcc736bd10ec6d02f872a1cd7034b6', N'+92 300 1234567', NULL, N'System Director', N'NGEOS Global Alliance', N'Admin', NULL, 1, N'2026-11-07 15:26:42.320', NULL);
GO
SET IDENTITY_INSERT [Users] OFF;
GO

-- Seed Data: [AboutPages] (7 rows)
SET IDENTITY_INSERT [AboutPages] ON;
GO
IF NOT EXISTS (SELECT 1 FROM [AboutPages] WHERE [Id] = 1)
    INSERT INTO [AboutPages] ([Id], [Slug], [Title], [Subtitle], [ContentHtml], [BannerImageUrl], [FeaturedImageUrl], [LastUpdated]) VALUES (1, N'what-we-do', N'What We Do', N'Transforming Vulnerable Communities Through Sustainable Humanitarian Aid', N'
                            <h3>Empowering Lives Across Every Sector of Need</h3>
                            <p class=''lead''>Give-AID serves as a unified umbrella platform connecting verified non-governmental organizations with compassionate global citizens and corporate donors.</p>
                            <div class=''row g-4 my-4''>
                                <div class=''col-md-6''>
                                    <div class=''p-3 bg-light rounded-3 border-start border-4 border-warning''>
                                        <h5><i class=''bi bi-heart-pulse-fill text-warning me-2''></i>Child & Orphan Welfare</h5>
                                        <p class=''small text-muted mb-0''>Ensuring orphaned and vulnerable children have safe housing, nourishing meals, trauma-informed counseling, and uninterrupted access to primary education.</p>
                                    </div>
                                </div>
                                <div class=''col-md-6''>
                                    <div class=''p-3 bg-light rounded-3 border-start border-4 border-primary''>
                                        <h5><i class=''bi bi-book-fill text-primary me-2''></i>Universal Education</h5>
                                        <p class=''small text-muted mb-0''>Supplying learning kits, modern solar-powered computer labs, and scholarships to eliminate poverty through high-quality literacy.</p>
                                    </div>
                                </div>
                                <div class=''col-md-6''>
                                    <div class=''p-3 bg-light rounded-3 border-start border-4 border-danger''>
                                        <h5><i class=''bi bi-hospital-fill text-danger me-2''></i>Emergency & Community Health</h5>
                                        <p class=''small text-muted mb-0''>Operating mobile diagnostic vans, pediatric surgeries, maternal health programs, and delivering essential medicines directly to marginalized villages.</p>
                                    </div>
                                </div>
                                <div class=''col-md-6''>
                                    <div class=''p-3 bg-light rounded-3 border-start border-4 border-success''>
                                        <h5><i class=''bi bi-person-wheelchair text-success me-2''></i>Disabled Persons Support</h5>
                                        <p class=''small text-muted mb-0''>Providing custom prosthetics, motorized wheelchairs, adaptive sports festivals, and accessibility equipment for specially challenged youth.</p>
                                    </div>
                                </div>
                            </div>
                            <p>Through stringent due diligence, 100% transparency in fund allocation, and real-time transaction tracking, Give-AID ensures every single dollar reaches the individuals who need it most.</p>
                        ', N'https://images.unsplash.com/photo-1488521787991-ed7bbaae773c?w=1200&auto=format&fit=crop&q=80', NULL, N'2026-10-24 18:16:36.766');
GO
IF NOT EXISTS (SELECT 1 FROM [AboutPages] WHERE [Id] = 2)
    INSERT INTO [AboutPages] ([Id], [Slug], [Title], [Subtitle], [ContentHtml], [BannerImageUrl], [FeaturedImageUrl], [LastUpdated]) VALUES (2, N'our-mission', N'Our Mission & Vision', N'Building a Compassionate World Free From Poverty, Illness, and Disparity', N'
                            <h3>Our Core Mission</h3>
                            <p class=''lead''>To mobilize global resources, champion high-impact grassroots NGOs, and create transparent, direct channels for donors to uplift vulnerable children, women, disabled persons, and elderly citizens worldwide.</p>
                            <h4 class=''mt-4''>Our Vision</h4>
                            <p>A just, equitable society where every human being—regardless of socio-economic background, physical ability, or geographic location—enjoys access to quality education, world-class healthcare, dignity, and economic opportunity.</p>
                            <h4 class=''mt-4''>Our Core Values</h4>
                            <ul class=''list-unstyled''>
                                <li class=''mb-2''><strong>1. Radical Transparency:</strong> Every dollar donated is accounted for with detailed impact reports and public transaction validation.</li>
                                <li class=''mb-2''><strong>2. Unwavering Empathy:</strong> We serve with unconditional compassion, placing human dignity at the center of all initiatives.</li>
                                <li class=''mb-2''><strong>3. Sustainable Impact:</strong> Rather than temporary handouts, we prioritize long-term skills, infrastructure, and self-reliance.</li>
                                <li class=''mb-2''><strong>4. Inclusive Collaboration:</strong> We bridge civil society, NGOs, philanthropic corporations, and volunteers to multiply our positive impact.</li>
                            </ul>
                        ', N'https://images.unsplash.com/photo-1542810634-71277d95dcbb?w=1200&auto=format&fit=crop&q=80', NULL, N'2026-10-24 18:16:36.767');
GO
IF NOT EXISTS (SELECT 1 FROM [AboutPages] WHERE [Id] = 3)
    INSERT INTO [AboutPages] ([Id], [Slug], [Title], [Subtitle], [ContentHtml], [BannerImageUrl], [FeaturedImageUrl], [LastUpdated]) VALUES (3, N'our-team', N'Our Team', N'Passionate Humanitarians, Experienced Leaders, and Dedicated Field Directors', N'
                            <h3>The People Behind Give-AID</h3>
                            <p class=''lead''>Our interdisciplinary team brings decades of combined experience across international relief, healthcare logistics, education management, and humanitarian governance.</p>
                            <p>We work tirelessly across our central secretariat and field partner hubs to ensure that all welfare programmes are executed with the highest operational excellence and ethical integrity.</p>
                        ', N'https://images.unsplash.com/photo-1522071820081-009f0129c71c?w=1200&auto=format&fit=crop&q=80', NULL, N'2026-10-24 18:16:36.767');
GO
IF NOT EXISTS (SELECT 1 FROM [AboutPages] WHERE [Id] = 4)
    INSERT INTO [AboutPages] ([Id], [Slug], [Title], [Subtitle], [ContentHtml], [BannerImageUrl], [FeaturedImageUrl], [LastUpdated]) VALUES (4, N'career-with-us', N'Career With Us', N'Turn Your Passion for Social Justice into a Fulfilling Career', N'
                            <h3>Join Our Mission-Driven Organization</h3>
                            <p class=''lead''>At Give-AID, we believe that the best work happens when passionate, skilled individuals come together to solve some of the world''s most pressing challenges.</p>
                            <p>We offer competitive compensation, comprehensive health benefits, flexible remote-friendly work environments, and the unbeatable satisfaction of seeing the direct human impact of your daily efforts.</p>
                            <h4 class=''mt-4''>Why Work With Give-AID?</h4>
                            <div class=''row g-3 my-2''>
                                <div class=''col-md-4''><div class=''p-3 bg-light rounded text-center''><h6>Impact-First Culture</h6><p class=''small text-muted mb-0''>Make a tangible difference in thousands of lives every day.</p></div></div>
                                <div class=''col-md-4''><div class=''p-3 bg-light rounded text-center''><h6>Growth & Mentorship</h6><p class=''small text-muted mb-0''>Continuous professional development and leadership coaching.</p></div></div>
                                <div class=''col-md-4''><div class=''p-3 bg-light rounded text-center''><h6>Inclusive Workplace</h6><p class=''small text-muted mb-0''>A diverse, collaborative global team driven by shared empathy.</p></div></div>
                            </div>
                        ', N'https://images.unsplash.com/photo-1531482615713-2afd69097998?w=1200&auto=format&fit=crop&q=80', NULL, N'2026-10-24 18:16:36.767');
GO
IF NOT EXISTS (SELECT 1 FROM [AboutPages] WHERE [Id] = 5)
    INSERT INTO [AboutPages] ([Id], [Slug], [Title], [Subtitle], [ContentHtml], [BannerImageUrl], [FeaturedImageUrl], [LastUpdated]) VALUES (5, N'our-achievements', N'Our Achievements', N'Milestones, Lives Impacted & Global Humanitarian Recognition', N'
                            <h3>A Decade of Transformational Humanitarian Impact</h3>
                            <p class=''lead''>Since our inception, Give-AID has mobilized millions of dollars in philanthropic funding to change lives across 45 communities worldwide.</p>
                            <div class=''row g-4 my-4 text-center''>
                                <div class=''col-6 col-md-3''><div class=''p-4 bg-light rounded-3 shadow-sm''><h2 class=''text-warning fw-bold mb-1''>50K+</h2><p class=''text-muted small mb-0''>Lives Directly Uplifted</p></div></div>
                                <div class=''col-6 col-md-3''><div class=''p-4 bg-light rounded-3 shadow-sm''><h2 class=''text-primary fw-bold mb-1''>$4.2M</h2><p class=''text-muted small mb-0''>Total Funds Mobilized</p></div></div>
                                <div class=''col-6 col-md-3''><div class=''p-4 bg-light rounded-3 shadow-sm''><h2 class=''text-success fw-bold mb-1''>35+</h2><p class=''text-muted small mb-0''>Partner NGOs Supported</p></div></div>
                                <div class=''col-6 col-md-3''><div class=''p-4 bg-light rounded-3 shadow-sm''><h2 class=''text-danger fw-bold mb-1''>100%</h2><p class=''text-muted small mb-0''>Audited Transparency</p></div></div>
                            </div>
                            <h4>Key Milestones</h4>
                            <ul>
                                <li><strong>2021:</strong> Launched the Rural Smart Classroom Initiative, equipping 50 schools with solar power.</li>
                                <li><strong>2023:</strong> Awarded the International Humanitarian Excellence Citation for fast medical emergency deployments.</li>
                                <li><strong>2025:</strong> Cross-funded over 120 custom prosthetic limbs for disabled youth in underserved regions.</li>
                                <li><strong>2026:</strong> Expanded Give-AID unified digital donation portal with real-time NGO transparency.</li>
                            </ul>
                        ', N'https://images.unsplash.com/photo-1511578314322-379afb476865?w=1200&auto=format&fit=crop&q=80', NULL, N'2026-10-24 18:16:36.767');
GO
IF NOT EXISTS (SELECT 1 FROM [AboutPages] WHERE [Id] = 6)
    INSERT INTO [AboutPages] ([Id], [Slug], [Title], [Subtitle], [ContentHtml], [BannerImageUrl], [FeaturedImageUrl], [LastUpdated]) VALUES (6, N'our-supporters', N'Our Supporters', N'Honoring the Generous Corporate Donors, Philanthropic Foundations & Allies', N'
                            <h3>Standing Strong Together With Our Benefactors</h3>
                            <p class=''lead''>Give-AID has been established and continuously powered by the generous contributions of major global corporations, philanthropic trusts, and visionary individual donors.</p>
                            <p>Their corporate social responsibility (CSR) investments fund operational infrastructure, technology tools, logistics fleets, and emergency match-funds, ensuring maximum impact for grassroots causes.</p>
                            <p>We extend our deepest gratitude to our corporate sponsors, institutional partners, and every individual donor who stands beside us in our mission to bring hope to humanity.</p>
                        ', N'https://images.unsplash.com/photo-1559526324-4b87b5e36e44?w=1200&auto=format&fit=crop&q=80', NULL, N'2026-10-24 18:16:36.767');
GO
IF NOT EXISTS (SELECT 1 FROM [AboutPages] WHERE [Id] = 7)
    INSERT INTO [AboutPages] ([Id], [Slug], [Title], [Subtitle], [ContentHtml], [BannerImageUrl], [FeaturedImageUrl], [LastUpdated]) VALUES (7, N'read-about-us', N'Read About Give-AID', N'The Comprehensive Story of Give-AID''s Founding, Growth, and Future', N'
                            <h3>The Story of Give-AID</h3>
                            <p class=''lead''>Give-AID was founded with a single powerful realization: while millions of generous individuals want to help the underprivileged, they often lack a trusted, transparent, and unified bridge to verify and support genuine grassroots NGOs.</p>
                            <p>Established as a centralized welfare ecosystem, Give-AID brings together accredited non-governmental organizations dedicated to children welfare, education literacy, emergency healthcare, disabled assistance, women empowerment, and senior citizen care.</p>
                            <h4>How Give-AID Works</h4>
                            <ol>
                                <li><strong>Rigorous NGO Accreditation:</strong> Only verified NGOs with proven track records, legal registrations, and financial integrity are onboarded.</li>
                                <li><strong>Targeted Welfare Causes:</strong> Causes are categorized clearly so donors can directly fund what they care about most.</li>
                                <li><strong>100% Direct Allocation:</strong> Transparent donation tracking ensures funds are allocated directly to planned activities and medical interventions.</li>
                                <li><strong>Holistic Community Engagement:</strong> Citizens can donate, join volunteer programmes, raise queries, invite friends, and participate in community charity events.</li>
                            </ol>
                            <p>Join us as we continue to expand our reach, innovate in charitable technology, and transform thousands more lives every single day.</p>
                        ', N'https://images.unsplash.com/photo-1469571486292-0ba58a3f068b?w=1200&auto=format&fit=crop&q=80', NULL, N'2026-10-24 18:16:36.767');
GO
SET IDENTITY_INSERT [AboutPages] OFF;
GO

-- Seed Data: [NGOs] (6 rows)
SET IDENTITY_INSERT [NGOs] ON;
GO
IF NOT EXISTS (SELECT 1 FROM [NGOs] WHERE [Id] = 1)
    INSERT INTO [NGOs] ([Id], [Name], [RegistrationNumber], [ShortDescription], [FullDescription], [LogoUrl], [BannerUrl], [FocusArea], [Website], [Email], [Phone], [Address], [EstablishedYear], [IsVerified], [IsActive], [CreatedAt]) VALUES (1, N'Hope For Children Foundation', N'NGO-88210-CH', N'Dedicated to child welfare, education, nutritional support and protecting vulnerable orphans worldwide.', N'Hope For Children Foundation is an international non-profit working across 14 nations to provide safe shelters, nutrition, quality education, and medical healthcare for vulnerable and orphaned children.', N'https://images.unsplash.com/photo-1488521787991-ed7bbaae773c?w=400&auto=format&fit=crop&q=80', N'https://images.unsplash.com/photo-1542810634-71277d95dcbb?w=1200&auto=format&fit=crop&q=80', N'Children Welfare & Protection', N'https://hopeforchildren.example.org', N'contact@hopeforchildren.org', N'+1 (555) 349-8821', N'45 Hope Street, Boston, MA', 2012, 1, 1, N'2026-10-24 18:16:32.843');
GO
IF NOT EXISTS (SELECT 1 FROM [NGOs] WHERE [Id] = 2)
    INSERT INTO [NGOs] ([Id], [Name], [RegistrationNumber], [ShortDescription], [FullDescription], [LogoUrl], [BannerUrl], [FocusArea], [Website], [Email], [Phone], [Address], [EstablishedYear], [IsVerified], [IsActive], [CreatedAt]) VALUES (2, N'Bright Future Education Trust', N'NGO-44109-ED', N'Empowering underprivileged children and youth through quality schooling, digital labs and scholarships.', N'Bright Future Education Trust builds schools, distributes learning kits, and trains teachers in rural and disadvantaged communities to break the cycle of poverty through education.', N'https://images.unsplash.com/photo-1509062522246-3755977927d7?w=400&auto=format&fit=crop&q=80', N'https://images.unsplash.com/photo-1497633762265-9d179a990aa6?w=1200&auto=format&fit=crop&q=80', N'Education & Literacy', N'https://brightfuture.example.org', N'info@brightfuturetrust.org', N'+1 (555) 782-4410', N'128 Academy Lane, Chicago, IL', 2015, 1, 1, N'2026-10-24 18:16:32.846');
GO
IF NOT EXISTS (SELECT 1 FROM [NGOs] WHERE [Id] = 3)
    INSERT INTO [NGOs] ([Id], [Name], [RegistrationNumber], [ShortDescription], [FullDescription], [LogoUrl], [BannerUrl], [FocusArea], [Website], [Email], [Phone], [Address], [EstablishedYear], [IsVerified], [IsActive], [CreatedAt]) VALUES (3, N'Global Health Aid Initiative', N'NGO-77123-HL', N'Providing emergency medical relief, mobile clinic caravans, and vital surgeries to remote communities.', N'Global Health Aid Initiative operates mobile surgical units, organizes free medical camps, and delivers essential medicines to poverty-stricken areas lacking basic healthcare infrastructure.', N'https://images.unsplash.com/photo-1576091160399-112ba8d25d1d?w=400&auto=format&fit=crop&q=80', N'https://images.unsplash.com/photo-1584515979956-d9f6e5d09982?w=1200&auto=format&fit=crop&q=80', N'Healthcare & Medical Relief', N'https://globalhealthaid.example.org', N'help@globalhealthaid.org', N'+1 (555) 901-7712', N'720 Medical Plaza, Atlanta, GA', 2014, 1, 1, N'2026-10-24 18:16:32.846');
GO
IF NOT EXISTS (SELECT 1 FROM [NGOs] WHERE [Id] = 4)
    INSERT INTO [NGOs] ([Id], [Name], [RegistrationNumber], [ShortDescription], [FullDescription], [LogoUrl], [BannerUrl], [FocusArea], [Website], [Email], [Phone], [Address], [EstablishedYear], [IsVerified], [IsActive], [CreatedAt]) VALUES (4, N'Ability & Beyond Foundation', N'NGO-33190-DS', N'Championing accessibility, prosthetic care, and vocational training for specially challenged and disabled individuals.', N'Ability & Beyond Foundation equips individuals with physical, auditory, and visual challenges with assistive tech, mobility wheelchairs, prosthetics, and sustainable job opportunities.', N'https://www.humanity-inclusion.org.uk/sn_uploads/fck/_DSC0061.jpg', NULL, N'Disabled Persons Welfare', N'https://abilityandbeyond.example.org', N'care@abilitybeyond.org', N'+1 (555) 412-3319', N'88 Inclusion Way, Seattle, WA', 2017, 1, 1, N'2026-10-24 18:16:32.000');
GO
IF NOT EXISTS (SELECT 1 FROM [NGOs] WHERE [Id] = 5)
    INSERT INTO [NGOs] ([Id], [Name], [RegistrationNumber], [ShortDescription], [FullDescription], [LogoUrl], [BannerUrl], [FocusArea], [Website], [Email], [Phone], [Address], [EstablishedYear], [IsVerified], [IsActive], [CreatedAt]) VALUES (5, N'Empower Women Global', N'NGO-99314-WM', N'Fostering female leadership, vocational skills, legal aid, and micro-grants for marginalized women.', N'Empower Women Global runs community centers teaching tailoring, digital design, financial literacy, and provides micro-seed capital to help women become financially independent.', N'https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?w=400&auto=format&fit=crop&q=80', N'https://images.unsplash.com/photo-1529156069898-49953e39b3ac?w=1200&auto=format&fit=crop&q=80', N'Women Welfare & Empowerment', N'https://empowerwomen.example.org', N'info@empowerwomen.org', N'+1 (555) 633-9931', N'500 Horizon Blvd, Austin, TX', 2016, 1, 1, N'2026-10-24 18:16:32.846');
GO
IF NOT EXISTS (SELECT 1 FROM [NGOs] WHERE [Id] = 6)
    INSERT INTO [NGOs] ([Id], [Name], [RegistrationNumber], [ShortDescription], [FullDescription], [LogoUrl], [BannerUrl], [FocusArea], [Website], [Email], [Phone], [Address], [EstablishedYear], [IsVerified], [IsActive], [CreatedAt]) VALUES (6, N'Silver Years Care Society', N'NGO-22108-EL', N'Providing comfortable shelter, daily companionship, and specialized geriatric care for abandoned seniors.', N'Silver Years Care Society operates dignified assisted-living homes, medical outreach, and community wellness programs ensuring our elders live their golden years with peace and respect.', N'https://images.unsplash.com/photo-1516307365426-bea591f05011?w=400&auto=format&fit=crop&q=80', N'https://images.unsplash.com/photo-1581579438747-1dc8d17bbce4?w=1200&auto=format&fit=crop&q=80', N'Elderly Care & Senior Welfare', N'https://silveryearscare.example.org', N'support@silveryears.org', N'+1 (555) 219-2210', N'310 Magnolia Drive, Miami, FL', 2018, 1, 1, N'2026-10-24 18:16:32.846');
GO
SET IDENTITY_INSERT [NGOs] OFF;
GO

-- Seed Data: [Causes] (7 rows)
SET IDENTITY_INSERT [Causes] ON;
GO
IF NOT EXISTS (SELECT 1 FROM [Causes] WHERE [Id] = 1)
    INSERT INTO [Causes] ([Id], [Title], [ShortDescription], [FullDescription], [Category], [TargetAmount], [RaisedAmount], [ImageUrl], [IsFeatured], [IsActive], [NgoId], [CreatedAt]) VALUES (1, N'Nutritious Food & Health Support for 500 Orphan Children', N'Provide daily balanced meals, clean drinking water, and essential vitamins to 500 orphaned children in shelter homes.', N'Malnutrition impairs physical growth and cognitive development. This campaign ensures 500 orphaned children receive three hot, nutritious meals daily, regular pediatric screenings, clean water filtration, and vital health supplements for an entire year.', N'Children', N'45000.00', N'50896.00', N'https://images.unsplash.com/photo-1488521787991-ed7bbaae773c?w=800&auto=format&fit=crop&q=80', 1, 1, 1, N'2026-09-24 18:16:35.937');
GO
IF NOT EXISTS (SELECT 1 FROM [Causes] WHERE [Id] = 2)
    INSERT INTO [Causes] ([Id], [Title], [ShortDescription], [FullDescription], [Category], [TargetAmount], [RaisedAmount], [ImageUrl], [IsFeatured], [IsActive], [NgoId], [CreatedAt]) VALUES (2, N'Build Smart Digital Classrooms for Rural Students', N'Equip 12 rural community schools with solar power, interactive smartboards, laptops, and certified digital learning curriculum.', N'Bridge the rural-urban digital divide. Your donations provide modern computing hardware, offline digital encyclopedias, solar-powered generators, and certified coding/literacy instructors for over 1,800 rural students.', N'Education', N'60000.00', N'43800.00', N'https://images.unsplash.com/photo-1509062522246-3755977927d7?w=800&auto=format&fit=crop&q=80', 1, 1, 2, N'2026-09-29 18:16:35.938');
GO
IF NOT EXISTS (SELECT 1 FROM [Causes] WHERE [Id] = 3)
    INSERT INTO [Causes] ([Id], [Title], [ShortDescription], [FullDescription], [Category], [TargetAmount], [RaisedAmount], [ImageUrl], [IsFeatured], [IsActive], [NgoId], [CreatedAt]) VALUES (3, N'Prosthetic Limbs & Rehabilitation for Disabled Youth', N'Deliver custom prosthetic limbs, motorized wheelchairs, and physical therapy to 120 disabled young individuals.', N'Mobility is independence. By funding lightweight, modern prosthetic limbs and comprehensive physical rehabilitation, you give specially challenged youth the dignity to walk, attend school, and pursue their dreams.', N'Disabled Persons', N'38000.00', N'25000.00', N'https://www.humanity-inclusion.org.uk/sn_uploads/fck/_DSC0061.jpg', 1, 1, 4, N'2026-10-04 18:16:35.000');
GO
IF NOT EXISTS (SELECT 1 FROM [Causes] WHERE [Id] = 4)
    INSERT INTO [Causes] ([Id], [Title], [ShortDescription], [FullDescription], [Category], [TargetAmount], [RaisedAmount], [ImageUrl], [IsFeatured], [IsActive], [NgoId], [CreatedAt]) VALUES (4, N'Mobile Health Clinics for Remote Tribal Villages', N'Deploy 2 fully-equipped mobile medical vans with diagnostic labs and medications to reach 25 remote villages weekly.', N'Hundreds of remote families live hours away from the nearest clinic. Our mobile vans bring doctors, nurses, ultrasound diagnostics, maternal checkups, and life-saving treatments right to their doorsteps.', N'Healthcare', N'52000.00', N'37150.00', N'https://images.unsplash.com/photo-1584515979956-d9f6e5d09982?w=800&auto=format&fit=crop&q=80', 0, 1, 3, N'2026-10-06 18:16:35.938');
GO
IF NOT EXISTS (SELECT 1 FROM [Causes] WHERE [Id] = 5)
    INSERT INTO [Causes] ([Id], [Title], [ShortDescription], [FullDescription], [Category], [TargetAmount], [RaisedAmount], [ImageUrl], [IsFeatured], [IsActive], [NgoId], [CreatedAt]) VALUES (5, N'Vocational Tailoring & Micro-Enterprise Grants for Women', N'Empower 250 low-income single mothers with sewing machines, business training, and seed capital to launch businesses.', N'Financial independence breaks domestic vulnerability. This program provides professional sewing equipment, digital e-commerce selling training, financial literacy workshops, and a $200 startup micro-grant to 250 determined women.', N'Women', N'28000.00', N'19600.00', N'https://images.unsplash.com/photo-1529156069898-49953e39b3ac?w=800&auto=format&fit=crop&q=80', 0, 1, 5, N'2026-10-09 18:16:35.938');
GO
IF NOT EXISTS (SELECT 1 FROM [Causes] WHERE [Id] = 6)
    INSERT INTO [Causes] ([Id], [Title], [ShortDescription], [FullDescription], [Category], [TargetAmount], [RaisedAmount], [ImageUrl], [IsFeatured], [IsActive], [NgoId], [CreatedAt]) VALUES (6, N'Warm Care, Medical Care & Meals for Abandoned Elderly', N'Sponsor warm shelter, daily geriatric healthcare, physical therapy, and nutritious meals for 85 abandoned senior citizens.', N'No senior citizen should face poverty, sickness, and loneliness alone. Your support provides clean bedding, warm clothing, specialized medications, nutritious soft meals, and compassionate social activities.', N'Elderly', N'32000.00', N'22425.00', N'https://images.unsplash.com/photo-1581579438747-1dc8d17bbce4?w=800&auto=format&fit=crop&q=80', 0, 1, 6, N'2026-10-12 18:16:35.938');
GO
IF NOT EXISTS (SELECT 1 FROM [Causes] WHERE [Id] = 7)
    INSERT INTO [Causes] ([Id], [Title], [ShortDescription], [FullDescription], [Category], [TargetAmount], [RaisedAmount], [ImageUrl], [IsFeatured], [IsActive], [NgoId], [CreatedAt]) VALUES (7, N'Youth Tech Skills & Career Apprenticeship Bootcamp', N'Train 300 disadvantaged youth in coding, graphic design, and customer support with guaranteed job placement assistance.', N'Empowering high-school dropouts and underprivileged youth with modern marketable tech skills, career mentorship, interview coaching, and paid apprenticeships in leading partner organizations.', N'Youth', N'40000.00', N'26500.00', N'https://images.unsplash.com/photo-1531482615713-2afd69097998?w=800&auto=format&fit=crop&q=80', 0, 1, 2, N'2026-10-14 18:16:35.938');
GO
SET IDENTITY_INSERT [Causes] OFF;
GO

-- Seed Data: [Programmes] (4 rows)
SET IDENTITY_INSERT [Programmes] ON;
GO
IF NOT EXISTS (SELECT 1 FROM [Programmes] WHERE [Id] = 1)
    INSERT INTO [Programmes] ([Id], [Title], [NgoId], [Category], [ShortDescription], [FullDescription], [ImageUrl], [StartDate], [EndDate], [Location], [TargetBeneficiaries], [IsActive], [CreatedAt]) VALUES (1, N'Education For Every Child: Smart Schooling Drive', 2, N'Education', N'Comprehensive learning initiative distributing 1,000 back-to-school kits, digital tablets, and school uniforms to rural kids.', N'Education is the foundation for lasting change. This 3-month programme equips underprivileged children with all necessary school supplies, access to digital e-learning libraries, and after-school remedial tutoring.', N'https://images.unsplash.com/photo-1497633762265-9d179a990aa6?w=800&auto=format&fit=crop&q=80', N'2026-11-07 18:16:36.380', N'2027-01-24 18:16:36.380', N'Metropolitan Convention Hall & 12 Rural Centers', 1000, 1, N'2026-10-24 18:16:36.379');
GO
IF NOT EXISTS (SELECT 1 FROM [Programmes] WHERE [Id] = 2)
    INSERT INTO [Programmes] ([Id], [Title], [NgoId], [Category], [ShortDescription], [FullDescription], [ImageUrl], [StartDate], [EndDate], [Location], [TargetBeneficiaries], [IsActive], [CreatedAt]) VALUES (2, N'Free Pediatric & Eye Care Medical Caravan', 3, N'Health Care', N'Free medical checkups, vision tests, corrective eyeglasses, and immunization shots for over 1,500 children.', N'Organized by volunteer doctors and pediatricians, this healthcare drive provides full diagnostic checkups, deworming medicines, corrective glasses, and urgent referrals for specialized pediatric surgeries.', N'https://images.unsplash.com/photo-1576091160399-112ba8d25d1d?w=800&auto=format&fit=crop&q=80', N'2026-11-14 18:16:36.381', N'2026-11-21 18:16:36.381', N'North District Community Center', 1500, 1, N'2026-10-24 18:16:36.381');
GO
IF NOT EXISTS (SELECT 1 FROM [Programmes] WHERE [Id] = 3)
    INSERT INTO [Programmes] ([Id], [Title], [NgoId], [Category], [ShortDescription], [FullDescription], [ImageUrl], [StartDate], [EndDate], [Location], [TargetBeneficiaries], [IsActive], [CreatedAt]) VALUES (3, N'Specially Challenged Children Art & Sports Festival', 4, N'Privileged children (Specially challenged)', N'A celebratory, inclusive festival featuring adaptive sports, sensory art workshops, and musical performances.', N'Celebrating the courage, creativity, and unique talents of children with special needs. The festival includes wheelchair races, adaptive painting competitions, sensory music therapy, and family counseling.', N'https://www.humanity-inclusion.org.uk/sn_uploads/fck/_DSC0061.jpg', N'2026-11-23 00:00:00.000', N'2026-11-25 00:00:00.000', N'Olympic City Sports Complex', 600, 1, N'2026-10-24 18:16:36.000');
GO
IF NOT EXISTS (SELECT 1 FROM [Programmes] WHERE [Id] = 4)
    INSERT INTO [Programmes] ([Id], [Title], [NgoId], [Category], [ShortDescription], [FullDescription], [ImageUrl], [StartDate], [EndDate], [Location], [TargetBeneficiaries], [IsActive], [CreatedAt]) VALUES (4, N'Women Tech & Digital Leadership Accelerator', 5, N'Women Empowerment', N'8-week intensive workshop teaching digital marketing, freelancing, and small business financial management to 200 women.', N'Equipping ambitious women from low-income communities with high-demand digital skills, business branding techniques, and micro-loan matching to foster independent sustainable livelihoods.', N'https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?w=800&auto=format&fit=crop&q=80', N'2026-12-03 18:16:36.381', N'2026-12-24 18:16:36.381', N'Give-AID Innovation Hub, Downtown', 200, 1, N'2026-10-24 18:16:36.381');
GO
SET IDENTITY_INSERT [Programmes] OFF;
GO

-- Seed Data: [Events] (3 rows)
SET IDENTITY_INSERT [Events] ON;
GO
IF NOT EXISTS (SELECT 1 FROM [Events] WHERE [Id] = 1)
    INSERT INTO [Events] ([Id], [Title], [ShortDescription], [FullDescription], [EventDate], [Location], [ImageUrl], [Organizer], [IsUpcoming], [CreatedAt]) VALUES (1, N'Annual Give-AID Global Charity Gala & Awards 2026', N'Join international philanthropists, civic leaders, and NGO heroes for an inspiring evening of celebration and fundraising.', N'Our flagship annual gala brings together over 500 compassionate donors, corporate partners, and grassroots workers to celebrate humanitarian milestones and raise crucial funding for the coming year.', N'2026-12-08 18:16:36.544', N'Grand Ballroom, Marriott Marquis Hotel, NY', N'https://images.unsplash.com/photo-1511578314322-379afb476865?w=800&auto=format&fit=crop&q=80', N'Give-AID Central Secretariat', 1, N'2026-10-24 18:16:36.543');
GO
IF NOT EXISTS (SELECT 1 FROM [Events] WHERE [Id] = 2)
    INSERT INTO [Events] ([Id], [Title], [ShortDescription], [FullDescription], [EventDate], [Location], [ImageUrl], [Organizer], [IsUpcoming], [CreatedAt]) VALUES (2, N'5K Charity Walk & Marathon for Children''s Education', N'Lace up your sneakers and run or walk to raise funds for school supplies and rural classrooms.', N'A family-friendly 5K run and community festival with live music, food stalls, and medals for all finishers. All registration fees go directly to our Education causes.', N'2026-11-18 18:16:36.545', N'Central City Waterfront Park', N'https://images.unsplash.com/photo-1530549387789-4c1017266635?w=800&auto=format&fit=crop&q=80', N'Bright Future Education Trust & Give-AID', 1, N'2026-10-24 18:16:36.545');
GO
IF NOT EXISTS (SELECT 1 FROM [Events] WHERE [Id] = 3)
    INSERT INTO [Events] ([Id], [Title], [ShortDescription], [FullDescription], [EventDate], [Location], [ImageUrl], [Organizer], [IsUpcoming], [CreatedAt]) VALUES (3, N'Healthcare for All: Community Health Expo & Blood Donation', N'Free wellness screenings, blood donation camp, CPR workshops, and pediatric consultations open to all.', N'Partnering with leading medical institutions to offer full health diagnostics, blood typing, voluntary blood donation drive, and child wellness nutrition consultations.', N'2026-11-08 18:16:36.545', N'Civic Plaza Pavilion', N'https://images.unsplash.com/photo-1584515979956-d9f6e5d09982?w=800&auto=format&fit=crop&q=80', N'Global Health Aid Initiative', 1, N'2026-10-24 18:16:36.545');
GO
SET IDENTITY_INSERT [Events] OFF;
GO

-- Seed Data: [GalleryItems] (5 rows)
SET IDENTITY_INSERT [GalleryItems] ON;
GO
IF NOT EXISTS (SELECT 1 FROM [GalleryItems] WHERE [Id] = 1)
    INSERT INTO [GalleryItems] ([Id], [Title], [Category], [ImageUrl], [Description], [EventDate], [CreatedAt]) VALUES (1, N'Distributing School Kits & Books in Rural Primary Schools', N'Education', N'https://images.unsplash.com/photo-1497633762265-9d179a990aa6?w=800&auto=format&fit=crop&q=80', N'Volunteers handing out backpacks, stationery, and books to eager elementary students.', N'2026-10-04 18:16:36.665', N'2026-10-24 18:16:36.663');
GO
IF NOT EXISTS (SELECT 1 FROM [GalleryItems] WHERE [Id] = 2)
    INSERT INTO [GalleryItems] ([Id], [Title], [Category], [ImageUrl], [Description], [EventDate], [CreatedAt]) VALUES (2, N'Free Eye Diagnostics & Corrective Spectacles Camp', N'Healthcare', N'https://images.unsplash.com/photo-1576091160399-112ba8d25d1d?w=800&auto=format&fit=crop&q=80', N'Medical team conducting detailed eye examinations for elderly villagers.', N'2026-09-19 18:16:36.666', N'2026-10-24 18:16:36.666');
GO
IF NOT EXISTS (SELECT 1 FROM [GalleryItems] WHERE [Id] = 3)
    INSERT INTO [GalleryItems] ([Id], [Title], [Category], [ImageUrl], [Description], [EventDate], [CreatedAt]) VALUES (3, N'Happy Smiles at the Annual Orphanage Food & Fun Carnival', N'Children', N'https://images.unsplash.com/photo-1488521787991-ed7bbaae773c?w=800&auto=format&fit=crop&q=80', N'Nutritious feasts, games, and gifts distributed to over 300 children.', N'2026-09-04 18:16:36.666', N'2026-10-24 18:16:36.666');
GO
IF NOT EXISTS (SELECT 1 FROM [GalleryItems] WHERE [Id] = 5)
    INSERT INTO [GalleryItems] ([Id], [Title], [Category], [ImageUrl], [Description], [EventDate], [CreatedAt]) VALUES (5, N'Women Tailoring & Vocational Academy Graduation', N'Events', N'https://images.unsplash.com/photo-1529156069898-49953e39b3ac?w=800&auto=format&fit=crop&q=80', N'Celebrating 80 female graduates receiving certified diplomas and brand-new sewing machines.', N'2026-10-09 18:16:36.666', N'2026-10-24 18:16:36.666');
GO
IF NOT EXISTS (SELECT 1 FROM [GalleryItems] WHERE [Id] = 6)
    INSERT INTO [GalleryItems] ([Id], [Title], [Category], [ImageUrl], [Description], [EventDate], [CreatedAt]) VALUES (6, N'Emergency Winter Blanket & Warm Clothing Distribution', N'Relief', N'https://images.unsplash.com/photo-1469571486292-0ba58a3f068b?w=800&auto=format&fit=crop&q=80', N'Delivering thermal blankets and warm clothes to elderly shelters during freezing winter.', N'2026-08-25 18:16:36.666', N'2026-10-24 18:16:36.666');
GO
SET IDENTITY_INSERT [GalleryItems] OFF;
GO

-- Seed Data: [JobOpportunities] (3 rows)
SET IDENTITY_INSERT [JobOpportunities] ON;
GO
IF NOT EXISTS (SELECT 1 FROM [JobOpportunities] WHERE [Id] = 1)
    INSERT INTO [JobOpportunities] ([Id], [Title], [Department], [JobType], [Location], [Description], [Requirements], [IsActive], [PostedDate]) VALUES (1, N'Regional Humanitarian Coordinator', N'Field Operations', N'Full Time', N'Northeast Region / Hybrid', N'Coordinate on-ground logistics for mobile health clinics, distribute relief supplies, and liaise with local partner NGOs.', N'Bachelor''s degree in Social Work or International Development, 3+ years NGO field experience, strong communication and emergency management skills.', 1, N'2026-10-14 18:16:36.986');
GO
IF NOT EXISTS (SELECT 1 FROM [JobOpportunities] WHERE [Id] = 2)
    INSERT INTO [JobOpportunities] ([Id], [Title], [Department], [JobType], [Location], [Description], [Requirements], [IsActive], [PostedDate]) VALUES (2, N'Digital Fundraising & Campaign Manager', N'Marketing & Communications', N'Full Time', N'Headquarters / Remote', N'Lead digital donor engagement strategies, manage social campaigns, and optimize fundraising donation funnels.', N'3+ years digital marketing experience in non-profit sector, proficiency in analytics, email marketing, and content storytelling.', 1, N'2026-10-16 18:16:36.986');
GO
IF NOT EXISTS (SELECT 1 FROM [JobOpportunities] WHERE [Id] = 3)
    INSERT INTO [JobOpportunities] ([Id], [Title], [Department], [JobType], [Location], [Description], [Requirements], [IsActive], [PostedDate]) VALUES (3, N'Volunteer Education & Mentorship Lead', N'Education Programs', N'Part Time', N'Various Rural Centers', N'Recruit, train, and guide volunteer tutors providing remedial classes and digital literacy to underprivileged children.', N'Background in teaching, education management or youth leadership. Passion for community service.', 1, N'2026-10-19 18:16:36.986');
GO
SET IDENTITY_INSERT [JobOpportunities] OFF;
GO

-- Seed Data: [Partners] (6 rows)
SET IDENTITY_INSERT [Partners] ON;
GO
IF NOT EXISTS (SELECT 1 FROM [Partners] WHERE [Id] = 1)
    INSERT INTO [Partners] ([Id], [Name], [Category], [LogoUrl], [WebsiteUrl], [Description], [DisplayOrder], [IsActive]) VALUES (1, N'Apex Global Technologies', N'Technology Partner', N'https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=300&auto=format&fit=crop&q=80', N'https://apexglobal.example.com', N'Supporting digital infrastructure, cloud services, and computer lab equipment for our education causes.', 1, 1);
GO
IF NOT EXISTS (SELECT 1 FROM [Partners] WHERE [Id] = 2)
    INSERT INTO [Partners] ([Id], [Name], [Category], [LogoUrl], [WebsiteUrl], [Description], [DisplayOrder], [IsActive]) VALUES (2, N'Horizon Healthcare Group', N'Healthcare Partner', N'https://images.unsplash.com/photo-1505751172876-fa1923c5c528?w=300&auto=format&fit=crop&q=80', N'https://horizonhealth.example.com', N'Donating medicines, surgical equipment, and medical volunteers for rural mobile clinic operations.', 2, 1);
GO
IF NOT EXISTS (SELECT 1 FROM [Partners] WHERE [Id] = 3)
    INSERT INTO [Partners] ([Id], [Name], [Category], [LogoUrl], [WebsiteUrl], [Description], [DisplayOrder], [IsActive]) VALUES (3, N'Starlight Financial Corp', N'Corporate Sponsor', N'https://images.unsplash.com/photo-1559526324-4b87b5e36e44?w=300&auto=format&fit=crop&q=80', N'https://starlightfin.example.com', N'Matching employee donations and funding micro-grant seed funds for women entrepreneurs.', 3, 1);
GO
IF NOT EXISTS (SELECT 1 FROM [Partners] WHERE [Id] = 4)
    INSERT INTO [Partners] ([Id], [Name], [Category], [LogoUrl], [WebsiteUrl], [Description], [DisplayOrder], [IsActive]) VALUES (4, N'Vanguard Logistics Alliance', N'Community Ally', N'https://images.unsplash.com/photo-1586528116311-ad8dd3c8310d?w=300&auto=format&fit=crop&q=80', N'https://vanguardlog.example.com', N'Providing free nationwide freight transport for emergency relief supplies and medical shipments.', 4, 1);
GO
IF NOT EXISTS (SELECT 1 FROM [Partners] WHERE [Id] = 5)
    INSERT INTO [Partners] ([Id], [Name], [Category], [LogoUrl], [WebsiteUrl], [Description], [DisplayOrder], [IsActive]) VALUES (5, N'Nexus Media Group', N'Media & Outreach', N'https://images.unsplash.com/photo-1557804506-669a67965ba0?w=300&auto=format&fit=crop&q=80', N'https://nexusmedia.example.com', N'Broadcasting public awareness campaigns and promoting Give-AID events pro-bono across national channels.', 5, 1);
GO
IF NOT EXISTS (SELECT 1 FROM [Partners] WHERE [Id] = 6)
    INSERT INTO [Partners] ([Id], [Name], [Category], [LogoUrl], [WebsiteUrl], [Description], [DisplayOrder], [IsActive]) VALUES (6, N'EcoGreen Energy Systems', N'Sustainability Sponsor', N'https://images.unsplash.com/photo-1497435334941-8c899ee9e8e9?w=300&auto=format&fit=crop&q=80', N'https://ecogreen.example.com', N'Installing clean solar power systems in rural schools and senior citizen shelter facilities.', 6, 1);
GO
SET IDENTITY_INSERT [Partners] OFF;
GO

-- Seed Data: [TeamMembers] (4 rows)
SET IDENTITY_INSERT [TeamMembers] ON;
GO
IF NOT EXISTS (SELECT 1 FROM [TeamMembers] WHERE [Id] = 1)
    INSERT INTO [TeamMembers] ([Id], [Name], [Role], [Bio], [PhotoUrl], [Email], [LinkedInUrl], [TwitterUrl], [DisplayOrder], [IsActive]) VALUES (1, N'Dr. Eleanor Vance', N'Executive Director & Founder', N'Former UN humanitarian advisor with over 18 years leading crisis relief and global child welfare initiatives.', N'https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?w=400&auto=format&fit=crop&q=80', N'e.vance@giveaid.org', N'https://linkedin.com/in/eleanor-vance-giveaid', NULL, 1, 1);
GO
IF NOT EXISTS (SELECT 1 FROM [TeamMembers] WHERE [Id] = 2)
    INSERT INTO [TeamMembers] ([Id], [Name], [Role], [Bio], [PhotoUrl], [Email], [LinkedInUrl], [TwitterUrl], [DisplayOrder], [IsActive]) VALUES (2, N'Marcus Sterling', N'Director of Field Operations & Logistics', N'Logistics expert specializing in disaster relief supply chains and mobile healthcare caravans across rural regions.', N'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=400&auto=format&fit=crop&q=80', N'm.sterling@giveaid.org', N'https://linkedin.com/in/marcus-sterling-giveaid', NULL, 2, 1);
GO
IF NOT EXISTS (SELECT 1 FROM [TeamMembers] WHERE [Id] = 3)
    INSERT INTO [TeamMembers] ([Id], [Name], [Role], [Bio], [PhotoUrl], [Email], [LinkedInUrl], [TwitterUrl], [DisplayOrder], [IsActive]) VALUES (3, N'Dr. Sophia Chen, MD', N'Chief Medical Officer', N'Pediatric surgeon and community health advocate leading our mobile clinics and free pediatric surgery drives.', N'https://images.unsplash.com/photo-1559839734-2b71ea197ec2?w=400&auto=format&fit=crop&q=80', N's.chen@giveaid.org', N'https://linkedin.com/in/sophia-chen-giveaid', NULL, 3, 1);
GO
IF NOT EXISTS (SELECT 1 FROM [TeamMembers] WHERE [Id] = 4)
    INSERT INTO [TeamMembers] ([Id], [Name], [Role], [Bio], [PhotoUrl], [Email], [LinkedInUrl], [TwitterUrl], [DisplayOrder], [IsActive]) VALUES (4, N'David Rodriguez', N'Head of NGO Partnerships & Compliance', N'Oversees due diligence, auditing, and quality assurance for all partner non-governmental organizations.', N'https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=400&auto=format&fit=crop&q=80', N'd.rodriguez@giveaid.org', N'https://linkedin.com/in/david-rodriguez-giveaid', NULL, 4, 1);
GO
SET IDENTITY_INSERT [TeamMembers] OFF;
GO

-- Seed Data: [FAQs] (6 rows)
SET IDENTITY_INSERT [FAQs] ON;
GO
IF NOT EXISTS (SELECT 1 FROM [FAQs] WHERE [Id] = 1)
    INSERT INTO [FAQs] ([Id], [Question], [Answer], [Category], [DisplayOrder], [IsActive]) VALUES (1, N'How do I make a donation on Give-AID?', N'You can browse our active causes under the ''Donate'' or ''Causes'' menu, select any cause of your choice (e.g. Children Welfare, Education, Healthcare, Disabled Persons), enter your desired amount, fill in your details and dummy payment card information, and submit. You will immediately receive a transaction receipt.', N'Donations & Payment', 1, 1);
GO
IF NOT EXISTS (SELECT 1 FROM [FAQs] WHERE [Id] = 2)
    INSERT INTO [FAQs] ([Id], [Question], [Answer], [Category], [DisplayOrder], [IsActive]) VALUES (2, N'Is this a real payment gateway?', N'No, Give-AID operates with a secure dummy payment simulation designed for university and educational demonstrations. No real money or actual card credentials are processed or stored.', N'Donations & Payment', 2, 1);
GO
IF NOT EXISTS (SELECT 1 FROM [FAQs] WHERE [Id] = 3)
    INSERT INTO [FAQs] ([Id], [Question], [Answer], [Category], [DisplayOrder], [IsActive]) VALUES (3, N'How can an NGO partner with Give-AID?', N'NGOs can apply for partnership by reaching out via our Contact Us page or submitting a partnership query. Our compliance team verifies registration documents, past audit statements, and field credibility before onboarding.', N'NGO Partnership', 3, 1);
GO
IF NOT EXISTS (SELECT 1 FROM [FAQs] WHERE [Id] = 4)
    INSERT INTO [FAQs] ([Id], [Question], [Answer], [Category], [DisplayOrder], [IsActive]) VALUES (4, N'How can I participate or volunteer in an upcoming programme?', N'Visit the ''Our Programmes'' section from the main navigation, select any programme (such as Education Drives, Healthcare Caravans, or Special Needs Festivals), and click ''Show Interest / Volunteer'' to submit your participation details.', N'Programmes & Volunteering', 4, 1);
GO
IF NOT EXISTS (SELECT 1 FROM [FAQs] WHERE [Id] = 5)
    INSERT INTO [FAQs] ([Id], [Question], [Answer], [Category], [DisplayOrder], [IsActive]) VALUES (5, N'Can I invite friends and colleagues to support a cause?', N'Yes! Registered users can use the ''Invite Friends'' feature from their dashboard or any cause page to send personalized email invitations to friends.', N'General', 5, 1);
GO
IF NOT EXISTS (SELECT 1 FROM [FAQs] WHERE [Id] = 6)
    INSERT INTO [FAQs] ([Id], [Question], [Answer], [Category], [DisplayOrder], [IsActive]) VALUES (6, N'How do I raise a query if I need help?', N'You can use the ''Raise Query'' button in the Help Centre or contact form. You can select your query category, submit your message, and track the Admin''s response directly from your user portal.', N'General', 6, 1);
GO
SET IDENTITY_INSERT [FAQs] OFF;
GO

-- Seed Data: [UserQueries] (1 rows)
SET IDENTITY_INSERT [UserQueries] ON;
GO
IF NOT EXISTS (SELECT 1 FROM [UserQueries] WHERE [Id] = 1)
    INSERT INTO [UserQueries] ([Id], [UserId], [FullName], [Email], [Subject], [Category], [Message], [AdminReply], [Status], [CreatedAt], [RepliedAt]) VALUES (1, NULL, N'Responsive Test User', N'testuser@example.com', N'Responsiveness Verification Inquiry', N'General', N'Testing admin query reply interface across mobile and desktop breakpoints.', NULL, N'Pending', N'2026-10-25 17:20:22.862', NULL);
GO
SET IDENTITY_INSERT [UserQueries] OFF;
GO

-- Seed Data: [Donations] (31 rows)
SET IDENTITY_INSERT [Donations] ON;
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 1)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (1, N'GA-TXN-202610-4D8DB9', 1, 2, N'SYED MUHAMMAD SAIM', N'syedmuhammadsaim.contact@gmail.com', NULL, N'50.00', N'Credit Card', N'SYED MUHAMMAD SAIM', N'****-****-****-4234', N'Completed', NULL, N'2026-10-27 08:04:33.361', NULL, N'Credit Card', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 2)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (2, N'GA-TXN-202610-415032', 4, 5, N'Abdullah Bukhari', N'abdull4h5599@gmail.com', N'+92 3242342343', N'50.00', N'Debit Card', N'Abdullah Bukhari', N'****-****-****-2342', N'Completed', N'[Type:General] [Email:Sent]', N'2026-10-27 18:47:03.203', NULL, N'Credit Card', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 3)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (3, N'GA-TXN-202610-730032', 3, 5, N'Abdullah Bukhari', N'abdull4h5599@gmail.com', N'+92 3424324323', N'50.00', N'Debit Card', N'Abdullah Bukhari', N'****-****-****-3253', N'Completed', N'[Type:Zakat] [Email:Sent]', N'2026-10-27 19:00:19.487', NULL, N'Credit Card', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 4)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (4, N'GA-TXN-202610-31514D', 3, 2, N'SYED MUHAMMAD SAIM', N'syedmuhammadsaim.contact@gmail.com', N'+92 4324234234', N'50.00', N'Debit Card', N'SYED MUHAMMAD SAIM', N'****-****-****-4342', N'Completed', N'[Type:General] [Email:Sent]', N'2026-10-27 19:17:56.269', NULL, N'Credit Card', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 5)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (5, N'GA-TXN-202610-9BAADF', 1, 2, N'SYED MUHAMMAD SAIM', N'syedmuhammadsaim.contact@gmail.com', N'+92 2342323423', N'27.00', N'Credit Card', N'SYED MUHAMMAD SAIM', N'****-****-****-2345', N'Completed', N'[Type:Zakat] [Email:Sent]', N'2026-10-27 21:19:45.125', NULL, N'Credit Card', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 6)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (6, N'GA-TXN-202610-834FB0', 1, 2, N'SYED MUHAMMAD SAIM', N'syedmuhammadsaim.contact@gmail.com', NULL, N'69.00', N'Credit Card', N'SYED MUHAMMAD SAIM', N'****-****-****-5645', N'Completed', N'[Type:Zakat] [Email:Sent]', N'2026-10-28 16:16:09.485', NULL, N'Credit Card', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 7)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (7, N'GA-TXN-202611-7FB8BC', 6, 8, N'saim', N'saim1@gmail.com', N'+92 2222222222', N'25.00', N'Debit Card', N'saim', N'****-****-****-5345', N'Completed', N'[Type:General] [Email:Failed]', N'2026-11-05 15:30:22.501', NULL, N'Credit Card', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 8)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (8, N'GA-TXN-202611-9F5D39', 1, 2, N'NGEOS Administrator', N'syedmuhammadsaim.contact@gmail.com', N'+92 2423423423', N'2500.00', N'Safepay', N'NGEOS Administrator', N'N/A (Hosted Gateway)', N'Pending', N'[Type:General] [Email:Pending] [Gateway:Safepay]', N'2026-11-07 18:28:43.985', NULL, N'Safepay', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 9)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (9, N'GA-TXN-202611-E383A2', 1, 2, N'NGEOS Administrator', N'syedmuhammadsaim.contact@gmail.com', N'+92 3242432432', N'1000.00', N'Easypaisa', N'NGEOS Administrator', N'N/A (Hosted Gateway)', N'Pending', N'[Type:Zakat] [Email:Pending] [Gateway:Easypaisa] sdfsdafsdfsdfsdf', N'2026-11-07 18:29:43.077', NULL, N'Easypaisa', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 10)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (10, N'GA-TXN-202611-D0F0FB', 1, 2, N'NGEOS Administrator', N'syedmuhammadsaim.contact@gmail.com', N'+92 4353453453', N'2500.00', N'Easypaisa', N'NGEOS Administrator', N'N/A (Hosted Gateway)', N'Pending', N'[Type:General] [Email:Pending] [Gateway:Easypaisa]', N'2026-11-07 18:40:25.413', NULL, N'Easypaisa', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 11)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (11, N'GA-TXN-202611-17FF5D', 1, 2, N'NGEOS Administrator', N'syedmuhammadsaim.contact@gmail.com', N'+92 3423423423', N'1000.00', N'JazzCash', N'NGEOS Administrator', N'N/A (Hosted Gateway)', N'Pending', N'[Type:General] [Email:Pending] [Gateway:JazzCash]', N'2026-11-07 19:21:05.904', NULL, N'JazzCash', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 12)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (12, N'GA-TXN-202611-20F191', 1, 9, N'Saim Admin', N'admin@ngeos.org', N'03001234567', N'1000.00', N'JazzCash', N'Saim Admin', N'N/A (Hosted Gateway)', N'Pending', N'[Type:General] [Email:Pending] [Gateway:JazzCash]', N'2026-11-07 19:25:31.021', NULL, N'JazzCash', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 13)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (13, N'GA-TXN-202611-E01978', 1, 9, N'Saim Admin', N'admin@ngeos.org', N'03001234567', N'1000.00', N'JazzCash', N'Saim Admin', N'N/A (Hosted Gateway)', N'Pending', N'[Type:General] [Email:Pending] [Gateway:JazzCash]', N'2026-11-07 19:25:47.670', NULL, N'JazzCash', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 14)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (14, N'GA-TXN-202611-E7A7CD', 1, 9, N'Saim Admin', N'admin@ngeos.org', N'03001234567', N'1000.00', N'JazzCash', N'Saim Admin', N'N/A (Hosted Gateway)', N'Pending', N'[Type:General] [Email:Pending] [Gateway:JazzCash]', N'2026-11-07 19:33:11.285', NULL, N'JazzCash', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 15)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (15, N'GA-TXN-202611-442964', 1, 2, N'NGEOS Administrator', N'syedmuhammadsaim.contact@gmail.com', N'+92 3434534543', N'1000.00', N'Easypaisa', N'NGEOS Administrator', N'N/A (Hosted Gateway)', N'Pending', N'[Type:General] [Email:Pending] [Gateway:Easypaisa]', N'2026-11-07 19:49:45.721', NULL, N'Easypaisa', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 16)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (16, N'GA-TXN-202611-1C61EC', 1, 2, N'NGEOS Administrator', N'syedmuhammadsaim.contact@gmail.com', N'+92 5758876768', N'1000.00', N'JazzCash', N'NGEOS Administrator', N'N/A (Hosted Gateway)', N'Pending', N'[Type:General] [Email:Pending] [Gateway:JazzCash]', N'2026-11-07 20:07:16.329', NULL, N'JazzCash', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 17)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (17, N'GA-TXN-202611-F1BE6A', 1, 2, N'NGEOS Administrator', N'syedmuhammadsaim.contact@gmail.com', NULL, N'1000.00', N'Easypaisa', N'NGEOS Administrator', N'N/A (Hosted Gateway)', N'Pending', N'[Type:General] [Email:Pending] [Gateway:Easypaisa]', N'2026-11-07 20:08:08.217', NULL, N'Easypaisa', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 18)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (18, N'GA-TXN-202611-2716DA', 1, 2, N'saim', N'syedmuhammadsaim.contact@gmail.com', N'+92 4543534534', N'2500.00', N'Safepay', N'saim', N'N/A (Hosted Gateway)', N'Pending', N'[Type:General] [Email:Pending] [Gateway:Safepay]', N'2026-11-07 20:11:10.122', NULL, N'Safepay', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 19)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (19, N'GA-TXN-202611-5043A8', 1, 2, N'saim', N'syedmuhammadsaim.contact@gmail.com', N'+92 4543534534', N'2500.00', N'PayFast', N'saim', N'N/A (Hosted Gateway)', N'Pending', N'[Type:General] [Email:Pending] [Gateway:PayFast]', N'2026-11-07 20:11:52.100', NULL, N'PayFast', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 20)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (20, N'GA-TXN-202611-F9F963', 1, 2, N'saim', N'syedmuhammadsaim.contact@gmail.com', N'+92 4543534534', N'2500.00', N'Easypaisa', N'saim', N'N/A (Hosted Gateway)', N'Pending', N'[Type:General] [Email:Pending] [Gateway:Easypaisa]', N'2026-11-07 20:12:11.111', NULL, N'Easypaisa', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 21)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (21, N'GA-TXN-202611-DD6028', 1, 9, N'Saim Test Donor', N'saim.test@example.com', N'03001234567', N'2500.00', N'JazzCash', N'Saim Test Donor', N'N/A (Hosted Gateway)', N'Completed', N'[Type:General] [Email:Pending] [Gateway:JazzCash]', N'2026-11-07 20:43:50.949', NULL, N'JazzCash', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 22)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (22, N'GA-TXN-202611-13E522', 1, 9, N'Saim Test Donor', N'saim.test@example.com', N'03001234567', N'2500.00', N'Easypaisa', N'Saim Test Donor', N'N/A (Hosted Gateway)', N'Pending', N'[Type:General] [Email:Pending] [Gateway:Easypaisa]', N'2026-11-07 20:43:55.981', NULL, N'Easypaisa', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 23)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (23, N'GA-TXN-202611-F5FC1C', 1, 9, N'Saim Test Donor', N'saim.test@example.com', N'03001234567', N'2500.00', N'Safepay', N'Saim Test Donor', N'N/A (Hosted Gateway)', N'Completed', N'[Type:General] [Email:Pending] [Gateway:Safepay]', N'2026-11-07 20:43:56.504', NULL, N'Safepay', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 24)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (24, N'GA-TXN-202611-E8F2CC', 1, 9, N'Saim Test Donor', N'saim.test@example.com', N'03001234567', N'2500.00', N'PayFast', N'Saim Test Donor', N'N/A (Hosted Gateway)', N'Failed', N'[Type:General] [Email:Pending] [Gateway:PayFast]', N'2026-11-07 20:44:00.826', NULL, N'PayFast', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 25)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (25, N'GA-TXN-202611-4A8FFC', 1, 9, N'Saim Test Donor', N'saim.test@example.com', N'03001234567', N'2500.00', N'JazzCash', N'Saim Test Donor', N'N/A (Hosted Gateway)', N'Completed', N'[Type:General] [Email:Pending] [Gateway:JazzCash]', N'2026-11-07 20:50:44.952', NULL, N'JazzCash', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 26)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (26, N'GA-TXN-202611-75916E', 1, 9, N'Saim Test Donor', N'saim.test@example.com', N'03001234567', N'2500.00', N'Easypaisa', N'Saim Test Donor', N'N/A (Hosted Gateway)', N'Completed', N'[Type:General] [Email:Pending] [Gateway:Easypaisa]', N'2026-11-07 20:50:48.075', NULL, N'Easypaisa', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 27)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (27, N'GA-TXN-202611-4ED1F5', 1, 9, N'Saim Test Donor', N'saim.test@example.com', N'03001234567', N'2500.00', N'Safepay', N'Saim Test Donor', N'N/A (Hosted Gateway)', N'Completed', N'[Type:General] [Email:Pending] [Gateway:Safepay]', N'2026-11-07 20:50:49.757', NULL, N'Safepay', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 28)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (28, N'GA-TXN-202611-C9A05B', 1, 9, N'Saim Test Donor', N'saim.test@example.com', N'03001234567', N'2500.00', N'PayFast', N'Saim Test Donor', N'N/A (Hosted Gateway)', N'Completed', N'[Type:General] [Email:Pending] [Gateway:PayFast]', N'2026-11-07 20:50:51.155', NULL, N'PayFast', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 29)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (29, N'GA-TXN-202611-A909F3', 1, 2, N'NGEOS Administrator', N'syedmuhammadsaim.contact@gmail.com', N'+92 4353563523', N'1000.00', N'Easypaisa', N'NGEOS Administrator', N'N/A (Hosted Gateway)', N'Completed', N'[Type:General] [Email:Pending] [Gateway:Easypaisa]', N'2026-11-07 20:53:06.588', NULL, N'Easypaisa', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 30)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (30, N'GA-TXN-202611-D15BD3', 1, 2, N'NGEOS Administrator', N'syedmuhammadsaim.contact@gmail.com', N'+92 3454353453', N'1000.00', N'PayFast', N'NGEOS Administrator', N'N/A (Hosted Gateway)', N'Completed', N'[Type:General] [Email:Pending] [Gateway:PayFast]', N'2026-11-07 21:10:44.021', NULL, N'PayFast', N'PKR');
GO
IF NOT EXISTS (SELECT 1 FROM [Donations] WHERE [Id] = 31)
    INSERT INTO [Donations] ([Id], [TransactionId], [CauseId], [UserId], [DonorName], [DonorEmail], [DonorPhone], [Amount], [CardType], [CardHolderName], [MaskedCardNumber], [PaymentStatus], [Notes], [DonationDate], [ProgrammeId], [PaymentGateway], [Currency]) VALUES (31, N'GA-TXN-202611-56EA75', 1, 9, N'Syed Muhammad Saim', N'syedmuhammadsaim.contact@gmail.com', NULL, N'2500.00', N'Easypaisa', N'Syed Muhammad Saim', N'N/A (Hosted Gateway)', N'Completed', N'[Type:General] [Email:Sent] [Gateway:Easypaisa]', N'2026-11-07 21:39:12.550', NULL, N'Easypaisa', N'PKR');
GO
SET IDENTITY_INSERT [Donations] OFF;
GO

-- Seed Data: [PaymentTransactions] (24 rows)
SET IDENTITY_INSERT [PaymentTransactions] ON;
GO
IF NOT EXISTS (SELECT 1 FROM [PaymentTransactions] WHERE [Id] = 1)
    INSERT INTO [PaymentTransactions] ([Id], [DonationId], [GatewayName], [InternalReference], [GatewayReference], [Amount], [Currency], [Status], [Environment], [VerificationStatus], [IdempotencyKey], [RawCallbackPayload], [RawWebhookPayload], [ErrorCode], [ErrorMessage], [RefundAmount], [RefundReference], [RefundReason], [RefundedAt], [CreatedAt], [CompletedAt]) VALUES (1, 8, N'Safepay', N'GA-TXN-202611-9F5D39', NULL, N'2500.00', N'PKR', N'Failed', N'Sandbox', N'Pending', N'fa08796a1740430cb680cab3a5a2a336', NULL, NULL, NULL, N'Payment gateway ''Safepay'' is not registered or supported in NGEOS.', NULL, NULL, NULL, NULL, N'2026-11-07 18:28:44.899', NULL);
GO
IF NOT EXISTS (SELECT 1 FROM [PaymentTransactions] WHERE [Id] = 2)
    INSERT INTO [PaymentTransactions] ([Id], [DonationId], [GatewayName], [InternalReference], [GatewayReference], [Amount], [Currency], [Status], [Environment], [VerificationStatus], [IdempotencyKey], [RawCallbackPayload], [RawWebhookPayload], [ErrorCode], [ErrorMessage], [RefundAmount], [RefundReference], [RefundReason], [RefundedAt], [CreatedAt], [CompletedAt]) VALUES (2, 9, N'Easypaisa', N'GA-TXN-202611-E383A2', NULL, N'1000.00', N'PKR', N'Failed', N'Sandbox', N'Pending', N'a36179747dc44fe891b5b9028d01c76e', NULL, NULL, NULL, N'Payment gateway ''Easypaisa'' is not registered or supported in NGEOS.', NULL, NULL, NULL, NULL, N'2026-11-07 18:29:43.105', NULL);
GO
IF NOT EXISTS (SELECT 1 FROM [PaymentTransactions] WHERE [Id] = 3)
    INSERT INTO [PaymentTransactions] ([Id], [DonationId], [GatewayName], [InternalReference], [GatewayReference], [Amount], [Currency], [Status], [Environment], [VerificationStatus], [IdempotencyKey], [RawCallbackPayload], [RawWebhookPayload], [ErrorCode], [ErrorMessage], [RefundAmount], [RefundReference], [RefundReason], [RefundedAt], [CreatedAt], [CompletedAt]) VALUES (3, 10, N'Easypaisa', N'GA-TXN-202611-D0F0FB', NULL, N'2500.00', N'PKR', N'Failed', N'Sandbox', N'Pending', N'874e2ea898d1453eb635ac81cec736ef', NULL, NULL, NULL, N'Payment gateway ''Easypaisa'' is not registered or supported in NGEOS.', NULL, NULL, NULL, NULL, N'2026-11-07 18:40:26.124', NULL);
GO
IF NOT EXISTS (SELECT 1 FROM [PaymentTransactions] WHERE [Id] = 4)
    INSERT INTO [PaymentTransactions] ([Id], [DonationId], [GatewayName], [InternalReference], [GatewayReference], [Amount], [Currency], [Status], [Environment], [VerificationStatus], [IdempotencyKey], [RawCallbackPayload], [RawWebhookPayload], [ErrorCode], [ErrorMessage], [RefundAmount], [RefundReference], [RefundReason], [RefundedAt], [CreatedAt], [CompletedAt]) VALUES (4, 11, N'JazzCash', N'GA-TXN-202611-17FF5D', N'T20261107192106948', N'1000.00', N'PKR', N'Pending', N'Sandbox', N'Pending', N'4dcf4c4f023b481dbdabd506eb400f7e', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-11-07 19:21:06.663', NULL);
GO
IF NOT EXISTS (SELECT 1 FROM [PaymentTransactions] WHERE [Id] = 5)
    INSERT INTO [PaymentTransactions] ([Id], [DonationId], [GatewayName], [InternalReference], [GatewayReference], [Amount], [Currency], [Status], [Environment], [VerificationStatus], [IdempotencyKey], [RawCallbackPayload], [RawWebhookPayload], [ErrorCode], [ErrorMessage], [RefundAmount], [RefundReference], [RefundReason], [RefundedAt], [CreatedAt], [CompletedAt]) VALUES (5, 12, N'JazzCash', N'GA-TXN-202611-20F191', N'T20261107192531049', N'1000.00', N'PKR', N'Pending', N'Sandbox', N'Pending', N'c82333d99af34bb2a47dddfae9a908b7', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-11-07 19:25:31.038', NULL);
GO
IF NOT EXISTS (SELECT 1 FROM [PaymentTransactions] WHERE [Id] = 6)
    INSERT INTO [PaymentTransactions] ([Id], [DonationId], [GatewayName], [InternalReference], [GatewayReference], [Amount], [Currency], [Status], [Environment], [VerificationStatus], [IdempotencyKey], [RawCallbackPayload], [RawWebhookPayload], [ErrorCode], [ErrorMessage], [RefundAmount], [RefundReference], [RefundReason], [RefundedAt], [CreatedAt], [CompletedAt]) VALUES (6, 13, N'JazzCash', N'GA-TXN-202611-E01978', N'T20261107192547692', N'1000.00', N'PKR', N'Pending', N'Sandbox', N'Pending', N'6fd8a81df2a04519979b7ef493e9fb2c', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-11-07 19:25:47.679', NULL);
GO
IF NOT EXISTS (SELECT 1 FROM [PaymentTransactions] WHERE [Id] = 7)
    INSERT INTO [PaymentTransactions] ([Id], [DonationId], [GatewayName], [InternalReference], [GatewayReference], [Amount], [Currency], [Status], [Environment], [VerificationStatus], [IdempotencyKey], [RawCallbackPayload], [RawWebhookPayload], [ErrorCode], [ErrorMessage], [RefundAmount], [RefundReference], [RefundReason], [RefundedAt], [CreatedAt], [CompletedAt]) VALUES (7, 14, N'JazzCash', N'GA-TXN-202611-E7A7CD', N'T20261108003312086', N'1000.00', N'PKR', N'Pending', N'Sandbox', N'Pending', N'74be5ab2a299484f820de0e46af509ef', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-11-07 19:33:11.859', NULL);
GO
IF NOT EXISTS (SELECT 1 FROM [PaymentTransactions] WHERE [Id] = 8)
    INSERT INTO [PaymentTransactions] ([Id], [DonationId], [GatewayName], [InternalReference], [GatewayReference], [Amount], [Currency], [Status], [Environment], [VerificationStatus], [IdempotencyKey], [RawCallbackPayload], [RawWebhookPayload], [ErrorCode], [ErrorMessage], [RefundAmount], [RefundReference], [RefundReason], [RefundedAt], [CreatedAt], [CompletedAt]) VALUES (8, 15, N'Easypaisa', N'GA-TXN-202611-442964', N'GA-TXN-202611-442964', N'1000.00', N'PKR', N'Pending', N'Sandbox', N'Pending', N'01bbf6d2808848c089067997be2f3fc1', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-11-07 19:49:46.388', NULL);
GO
IF NOT EXISTS (SELECT 1 FROM [PaymentTransactions] WHERE [Id] = 9)
    INSERT INTO [PaymentTransactions] ([Id], [DonationId], [GatewayName], [InternalReference], [GatewayReference], [Amount], [Currency], [Status], [Environment], [VerificationStatus], [IdempotencyKey], [RawCallbackPayload], [RawWebhookPayload], [ErrorCode], [ErrorMessage], [RefundAmount], [RefundReference], [RefundReason], [RefundedAt], [CreatedAt], [CompletedAt]) VALUES (9, 16, N'JazzCash', N'GA-TXN-202611-1C61EC', N'T20261108010717363', N'1000.00', N'PKR', N'Pending', N'Sandbox', N'Pending', N'1eb3d66959c64efaa548612b4a576d4b', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-11-07 20:07:17.013', NULL);
GO
IF NOT EXISTS (SELECT 1 FROM [PaymentTransactions] WHERE [Id] = 10)
    INSERT INTO [PaymentTransactions] ([Id], [DonationId], [GatewayName], [InternalReference], [GatewayReference], [Amount], [Currency], [Status], [Environment], [VerificationStatus], [IdempotencyKey], [RawCallbackPayload], [RawWebhookPayload], [ErrorCode], [ErrorMessage], [RefundAmount], [RefundReference], [RefundReason], [RefundedAt], [CreatedAt], [CompletedAt]) VALUES (10, 17, N'Easypaisa', N'GA-TXN-202611-F1BE6A', N'GA-TXN-202611-F1BE6A', N'1000.00', N'PKR', N'Pending', N'Sandbox', N'Pending', N'a71b5a5838fb4d85b1f982664e76bea6', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-11-07 20:08:08.245', NULL);
GO
IF NOT EXISTS (SELECT 1 FROM [PaymentTransactions] WHERE [Id] = 11)
    INSERT INTO [PaymentTransactions] ([Id], [DonationId], [GatewayName], [InternalReference], [GatewayReference], [Amount], [Currency], [Status], [Environment], [VerificationStatus], [IdempotencyKey], [RawCallbackPayload], [RawWebhookPayload], [ErrorCode], [ErrorMessage], [RefundAmount], [RefundReference], [RefundReason], [RefundedAt], [CreatedAt], [CompletedAt]) VALUES (11, 18, N'Safepay', N'GA-TXN-202611-2716DA', NULL, N'2500.00', N'PKR', N'Failed', N'Sandbox', N'Pending', N'cee0397071894689863d9fd04a6d9417', N'{"data":null,"status":{"errors":["Client with this identifier not found"],"message":"fail"}}
', NULL, N'HTTP_NotFound', N'Failed to create Safepay payment session.', NULL, NULL, NULL, NULL, N'2026-11-07 20:11:10.143', NULL);
GO
IF NOT EXISTS (SELECT 1 FROM [PaymentTransactions] WHERE [Id] = 12)
    INSERT INTO [PaymentTransactions] ([Id], [DonationId], [GatewayName], [InternalReference], [GatewayReference], [Amount], [Currency], [Status], [Environment], [VerificationStatus], [IdempotencyKey], [RawCallbackPayload], [RawWebhookPayload], [ErrorCode], [ErrorMessage], [RefundAmount], [RefundReference], [RefundReason], [RefundedAt], [CreatedAt], [CompletedAt]) VALUES (12, 19, N'PayFast', N'GA-TXN-202611-5043A8', NULL, N'2500.00', N'PKR', N'Failed', N'Sandbox', N'Pending', N'29a519f96f1c4bb9be8822ebfa47e786', N'{"Message":"An error has occurred.","ExceptionMessage":"Object reference not set to an instance of an object.","ExceptionType":"System.NullReferenceException","StackTrace":"   at APS.IPG.Controllers.TransactionController.GetAccessToken(AccessToken accessToken)\r\n   at lambda_method(Closure , Object , Object[] )\r\n   at System.Web.Http.Controllers.ReflectedHttpActionDescriptor.ActionExecutor.<>c__DisplayClass10.<GetExecutor>b__9(Object instance, Object[] methodParameters)\r\n   at System.Web.Http.Controllers.ReflectedHttpActionDescriptor.ExecuteAsync(HttpControllerContext controllerContext, IDictionary`2 arguments, CancellationToken cancellationToken)\r\n--- End of stack trace from previous location where exception was thrown ---\r\n   at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()\r\n   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)\r\n   at System.Web.Http.Controllers.ApiControllerActionInvoker.<InvokeActionAsyncCore>d__0.MoveNext()\r\n--- End of stack trace from previous location where exception was thrown ---\r\n   at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()\r\n   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)\r\n   at System.Web.Http.Controllers.ActionFilterResult.<ExecuteAsync>d__2.MoveNext()\r\n--- End of stack trace from previous location where exception was thrown ---\r\n   at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()\r\n   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)\r\n   at System.Web.Http.Dispatcher.HttpControllerDispatcher.<SendAsync>d__1.MoveNext()"}', NULL, N'HTTP_InternalServerError', N'Failed to obtain PayFast checkout token.', NULL, NULL, NULL, NULL, N'2026-11-07 20:11:52.123', NULL);
GO
IF NOT EXISTS (SELECT 1 FROM [PaymentTransactions] WHERE [Id] = 13)
    INSERT INTO [PaymentTransactions] ([Id], [DonationId], [GatewayName], [InternalReference], [GatewayReference], [Amount], [Currency], [Status], [Environment], [VerificationStatus], [IdempotencyKey], [RawCallbackPayload], [RawWebhookPayload], [ErrorCode], [ErrorMessage], [RefundAmount], [RefundReference], [RefundReason], [RefundedAt], [CreatedAt], [CompletedAt]) VALUES (13, 20, N'Easypaisa', N'GA-TXN-202611-F9F963', N'GA-TXN-202611-F9F963', N'2500.00', N'PKR', N'Pending', N'Sandbox', N'Pending', N'b1c6b17c3ba246a6b61a2d476ddc02b6', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-11-07 20:12:11.141', NULL);
GO
IF NOT EXISTS (SELECT 1 FROM [PaymentTransactions] WHERE [Id] = 14)
    INSERT INTO [PaymentTransactions] ([Id], [DonationId], [GatewayName], [InternalReference], [GatewayReference], [Amount], [Currency], [Status], [Environment], [VerificationStatus], [IdempotencyKey], [RawCallbackPayload], [RawWebhookPayload], [ErrorCode], [ErrorMessage], [RefundAmount], [RefundReference], [RefundReason], [RefundedAt], [CreatedAt], [CompletedAt]) VALUES (14, 21, N'JazzCash', N'GA-TXN-202611-DD6028', N'JC-SIM-A6877CDC', N'2500.00', N'PKR', N'Completed', N'Sandbox', N'Verified', N'5792ac8bc68f499386066b4ac8d0bfa4', N'', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-11-07 20:43:52.220', N'2026-11-07 20:43:53.123');
GO
IF NOT EXISTS (SELECT 1 FROM [PaymentTransactions] WHERE [Id] = 15)
    INSERT INTO [PaymentTransactions] ([Id], [DonationId], [GatewayName], [InternalReference], [GatewayReference], [Amount], [Currency], [Status], [Environment], [VerificationStatus], [IdempotencyKey], [RawCallbackPayload], [RawWebhookPayload], [ErrorCode], [ErrorMessage], [RefundAmount], [RefundReference], [RefundReason], [RefundedAt], [CreatedAt], [CompletedAt]) VALUES (15, 22, N'Easypaisa', N'GA-TXN-202611-13E522', N'GA-TXN-202611-13E522', N'2500.00', N'PKR', N'Pending', N'Sandbox', N'Pending', N'4a1bf4c4dd5346a896cab7abfb4c4c44', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-11-07 20:43:56.357', NULL);
GO
IF NOT EXISTS (SELECT 1 FROM [PaymentTransactions] WHERE [Id] = 16)
    INSERT INTO [PaymentTransactions] ([Id], [DonationId], [GatewayName], [InternalReference], [GatewayReference], [Amount], [Currency], [Status], [Environment], [VerificationStatus], [IdempotencyKey], [RawCallbackPayload], [RawWebhookPayload], [ErrorCode], [ErrorMessage], [RefundAmount], [RefundReference], [RefundReason], [RefundedAt], [CreatedAt], [CompletedAt]) VALUES (16, 23, N'Safepay', N'GA-TXN-202611-F5FC1C', N'SPY-SIM-76CEC321', N'2500.00', N'PKR', N'Completed', N'Sandbox', N'Verified', N'7c68243fd2f249bfac0f63ebb399710a', N'', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-11-07 20:43:56.562', N'2026-11-07 20:43:56.829');
GO
IF NOT EXISTS (SELECT 1 FROM [PaymentTransactions] WHERE [Id] = 17)
    INSERT INTO [PaymentTransactions] ([Id], [DonationId], [GatewayName], [InternalReference], [GatewayReference], [Amount], [Currency], [Status], [Environment], [VerificationStatus], [IdempotencyKey], [RawCallbackPayload], [RawWebhookPayload], [ErrorCode], [ErrorMessage], [RefundAmount], [RefundReference], [RefundReason], [RefundedAt], [CreatedAt], [CompletedAt]) VALUES (17, 24, N'PayFast', N'GA-TXN-202611-E8F2CC', N'PF-SIM-F44C07AD', N'2500.00', N'PKR', N'Failed', N'Sandbox', N'Failed', N'f55b9917b3214871a937ebe128affa53', N'', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-11-07 20:44:00.848', NULL);
GO
IF NOT EXISTS (SELECT 1 FROM [PaymentTransactions] WHERE [Id] = 18)
    INSERT INTO [PaymentTransactions] ([Id], [DonationId], [GatewayName], [InternalReference], [GatewayReference], [Amount], [Currency], [Status], [Environment], [VerificationStatus], [IdempotencyKey], [RawCallbackPayload], [RawWebhookPayload], [ErrorCode], [ErrorMessage], [RefundAmount], [RefundReference], [RefundReason], [RefundedAt], [CreatedAt], [CompletedAt]) VALUES (18, 25, N'JazzCash', N'GA-TXN-202611-4A8FFC', N'JC-SIM-A27781D0', N'2500.00', N'PKR', N'Completed', N'Sandbox', N'Verified', N'97c9bb40fcbb4d7ab839f16ade0911f3', N'', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-11-07 20:50:45.315', N'2026-11-07 20:50:45.795');
GO
IF NOT EXISTS (SELECT 1 FROM [PaymentTransactions] WHERE [Id] = 19)
    INSERT INTO [PaymentTransactions] ([Id], [DonationId], [GatewayName], [InternalReference], [GatewayReference], [Amount], [Currency], [Status], [Environment], [VerificationStatus], [IdempotencyKey], [RawCallbackPayload], [RawWebhookPayload], [ErrorCode], [ErrorMessage], [RefundAmount], [RefundReference], [RefundReason], [RefundedAt], [CreatedAt], [CompletedAt]) VALUES (19, 26, N'Easypaisa', N'GA-TXN-202611-75916E', N'EP-SIM-06480290', N'2500.00', N'PKR', N'Completed', N'Sandbox', N'Verified', N'1eafaa58eaa04b33ae8d892da5d692c9', N'', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-11-07 20:50:48.080', N'2026-11-07 20:50:48.131');
GO
IF NOT EXISTS (SELECT 1 FROM [PaymentTransactions] WHERE [Id] = 20)
    INSERT INTO [PaymentTransactions] ([Id], [DonationId], [GatewayName], [InternalReference], [GatewayReference], [Amount], [Currency], [Status], [Environment], [VerificationStatus], [IdempotencyKey], [RawCallbackPayload], [RawWebhookPayload], [ErrorCode], [ErrorMessage], [RefundAmount], [RefundReference], [RefundReason], [RefundedAt], [CreatedAt], [CompletedAt]) VALUES (20, 27, N'Safepay', N'GA-TXN-202611-4ED1F5', N'SPY-SIM-5278A742', N'2500.00', N'PKR', N'Completed', N'Sandbox', N'Verified', N'80f9117d361c46e4bfc578d669b68aab', N'', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-11-07 20:50:49.768', N'2026-11-07 20:50:49.824');
GO
IF NOT EXISTS (SELECT 1 FROM [PaymentTransactions] WHERE [Id] = 21)
    INSERT INTO [PaymentTransactions] ([Id], [DonationId], [GatewayName], [InternalReference], [GatewayReference], [Amount], [Currency], [Status], [Environment], [VerificationStatus], [IdempotencyKey], [RawCallbackPayload], [RawWebhookPayload], [ErrorCode], [ErrorMessage], [RefundAmount], [RefundReference], [RefundReason], [RefundedAt], [CreatedAt], [CompletedAt]) VALUES (21, 28, N'PayFast', N'GA-TXN-202611-C9A05B', N'PF-SIM-024F6AED', N'2500.00', N'PKR', N'Completed', N'Sandbox', N'Verified', N'd5a3888eea0b40a5af0d01765f57bef3', N'', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-11-07 20:50:51.165', N'2026-11-07 20:50:51.218');
GO
IF NOT EXISTS (SELECT 1 FROM [PaymentTransactions] WHERE [Id] = 22)
    INSERT INTO [PaymentTransactions] ([Id], [DonationId], [GatewayName], [InternalReference], [GatewayReference], [Amount], [Currency], [Status], [Environment], [VerificationStatus], [IdempotencyKey], [RawCallbackPayload], [RawWebhookPayload], [ErrorCode], [ErrorMessage], [RefundAmount], [RefundReference], [RefundReason], [RefundedAt], [CreatedAt], [CompletedAt]) VALUES (22, 29, N'Easypaisa', N'GA-TXN-202611-A909F3', N'EP-SIM-78AC9364', N'1000.00', N'PKR', N'Completed', N'Sandbox', N'Verified', N'c2a21c3a63964baf8df88d7b4fa0ce2d', N'', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-11-07 20:53:06.612', N'2026-11-07 20:53:21.898');
GO
IF NOT EXISTS (SELECT 1 FROM [PaymentTransactions] WHERE [Id] = 23)
    INSERT INTO [PaymentTransactions] ([Id], [DonationId], [GatewayName], [InternalReference], [GatewayReference], [Amount], [Currency], [Status], [Environment], [VerificationStatus], [IdempotencyKey], [RawCallbackPayload], [RawWebhookPayload], [ErrorCode], [ErrorMessage], [RefundAmount], [RefundReference], [RefundReason], [RefundedAt], [CreatedAt], [CompletedAt]) VALUES (23, 30, N'PayFast', N'GA-TXN-202611-D15BD3', N'PF-SIM-C67BEEB6', N'1000.00', N'PKR', N'Completed', N'Sandbox', N'Verified', N'b5957816e1124ff89de6c08e68b4b114', N'', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-11-07 21:10:44.798', N'2026-11-07 21:11:09.481');
GO
IF NOT EXISTS (SELECT 1 FROM [PaymentTransactions] WHERE [Id] = 24)
    INSERT INTO [PaymentTransactions] ([Id], [DonationId], [GatewayName], [InternalReference], [GatewayReference], [Amount], [Currency], [Status], [Environment], [VerificationStatus], [IdempotencyKey], [RawCallbackPayload], [RawWebhookPayload], [ErrorCode], [ErrorMessage], [RefundAmount], [RefundReference], [RefundReason], [RefundedAt], [CreatedAt], [CompletedAt]) VALUES (24, 31, N'Easypaisa', N'GA-TXN-202611-56EA75', N'EP-SIM-E1953AEB', N'2500.00', N'PKR', N'Completed', N'Sandbox', N'Verified', N'9aee3b7e46274fc28746402f74673d6e', N'', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-11-07 21:39:13.079', N'2026-11-07 21:39:13.406');
GO
SET IDENTITY_INSERT [PaymentTransactions] OFF;
GO

-- Seed Data: [ProgrammeInterests] (1 rows)
SET IDENTITY_INSERT [ProgrammeInterests] ON;
GO
IF NOT EXISTS (SELECT 1 FROM [ProgrammeInterests] WHERE [Id] = 1)
    INSERT INTO [ProgrammeInterests] ([Id], [ProgrammeId], [UserId], [FullName], [Email], [Phone], [Occupation], [InterestType], [Message], [Status], [SubmittedAt]) VALUES (1, 1, 2, N'SYED MUHAMMAD SAIM', N'syedmuhammadsaim.contact@gmail.com', N'+92 5434535355', N'Software', N'Sponsor', NULL, N'Pending', N'2026-10-27 20:21:13.001');
GO
SET IDENTITY_INSERT [ProgrammeInterests] OFF;
GO

-- Seed Data: [FriendInvitations] (1 rows)
SET IDENTITY_INSERT [FriendInvitations] ON;
GO
IF NOT EXISTS (SELECT 1 FROM [FriendInvitations] WHERE [Id] = 1)
    INSERT INTO [FriendInvitations] ([Id], [UserId], [SenderName], [SenderEmail], [FriendEmail], [CauseId], [PersonalMessage], [SentAt]) VALUES (1, 2, N'SYED MUHAMMAD SAIM', N'syedmuhammadsaim.contact@gmail.com', N'abdull4h5599@gmail.com', NULL, N'Hey! I''ve been supporting Give-AID and wanted to invite you to join this wonderful mission to help people in need. Check out their impactful causes!', N'2026-10-28 16:43:32.948');
GO
SET IDENTITY_INSERT [FriendInvitations] OFF;
GO

-- Seed Data: [PasswordResetTokens] (19 rows)
SET IDENTITY_INSERT [PasswordResetTokens] ON;
GO
IF NOT EXISTS (SELECT 1 FROM [PasswordResetTokens] WHERE [Id] = 1)
    INSERT INTO [PasswordResetTokens] ([Id], [UserId], [Code], [ResetSessionToken], [IsVerified], [Expiration], [IsUsed], [Attempts], [CreatedAt]) VALUES (1, 3, N'986918', NULL, 0, N'2026-10-25 09:45:44.429', 0, 0, N'2026-10-25 09:35:44.430');
GO
IF NOT EXISTS (SELECT 1 FROM [PasswordResetTokens] WHERE [Id] = 2)
    INSERT INTO [PasswordResetTokens] ([Id], [UserId], [Code], [ResetSessionToken], [IsVerified], [Expiration], [IsUsed], [Attempts], [CreatedAt]) VALUES (2, 2, N'866097', NULL, 0, N'2026-10-25 09:51:01.638', 1, 0, N'2026-10-25 09:41:01.639');
GO
IF NOT EXISTS (SELECT 1 FROM [PasswordResetTokens] WHERE [Id] = 3)
    INSERT INTO [PasswordResetTokens] ([Id], [UserId], [Code], [ResetSessionToken], [IsVerified], [Expiration], [IsUsed], [Attempts], [CreatedAt]) VALUES (3, 2, N'323869', N'5839386d4d654e9e8465ac683b6c218c', 1, N'2026-10-25 10:01:38.391', 1, 0, N'2026-10-25 09:51:38.393');
GO
IF NOT EXISTS (SELECT 1 FROM [PasswordResetTokens] WHERE [Id] = 4)
    INSERT INTO [PasswordResetTokens] ([Id], [UserId], [Code], [ResetSessionToken], [IsVerified], [Expiration], [IsUsed], [Attempts], [CreatedAt]) VALUES (4, 2, N'320452', N'3bf93d7da08944398eb8264b57ea8b41', 1, N'2026-10-25 10:13:58.597', 1, 0, N'2026-10-25 10:03:58.599');
GO
IF NOT EXISTS (SELECT 1 FROM [PasswordResetTokens] WHERE [Id] = 5)
    INSERT INTO [PasswordResetTokens] ([Id], [UserId], [Code], [ResetSessionToken], [IsVerified], [Expiration], [IsUsed], [Attempts], [CreatedAt]) VALUES (5, 2, N'441631', NULL, 0, N'2026-10-27 15:07:44.023', 1, 0, N'2026-10-27 14:57:44.024');
GO
IF NOT EXISTS (SELECT 1 FROM [PasswordResetTokens] WHERE [Id] = 6)
    INSERT INTO [PasswordResetTokens] ([Id], [UserId], [Code], [ResetSessionToken], [IsVerified], [Expiration], [IsUsed], [Attempts], [CreatedAt]) VALUES (6, 4, N'407852', NULL, 0, N'2026-10-27 15:13:31.612', 1, 0, N'2026-10-27 15:03:31.612');
GO
IF NOT EXISTS (SELECT 1 FROM [PasswordResetTokens] WHERE [Id] = 7)
    INSERT INTO [PasswordResetTokens] ([Id], [UserId], [Code], [ResetSessionToken], [IsVerified], [Expiration], [IsUsed], [Attempts], [CreatedAt]) VALUES (7, 4, N'643102', NULL, 0, N'2026-10-27 15:17:03.117', 0, 0, N'2026-10-27 15:07:03.117');
GO
IF NOT EXISTS (SELECT 1 FROM [PasswordResetTokens] WHERE [Id] = 8)
    INSERT INTO [PasswordResetTokens] ([Id], [UserId], [Code], [ResetSessionToken], [IsVerified], [Expiration], [IsUsed], [Attempts], [CreatedAt]) VALUES (8, 2, N'638495', NULL, 0, N'2026-10-27 16:45:48.753', 1, 0, N'2026-10-27 16:35:48.753');
GO
IF NOT EXISTS (SELECT 1 FROM [PasswordResetTokens] WHERE [Id] = 9)
    INSERT INTO [PasswordResetTokens] ([Id], [UserId], [Code], [ResetSessionToken], [IsVerified], [Expiration], [IsUsed], [Attempts], [CreatedAt]) VALUES (9, 2, N'414928', NULL, 0, N'2026-10-27 16:55:00.442', 1, 0, N'2026-10-27 16:45:00.443');
GO
IF NOT EXISTS (SELECT 1 FROM [PasswordResetTokens] WHERE [Id] = 10)
    INSERT INTO [PasswordResetTokens] ([Id], [UserId], [Code], [ResetSessionToken], [IsVerified], [Expiration], [IsUsed], [Attempts], [CreatedAt]) VALUES (10, 2, N'876973', N'55fdf88389704cd1b52674d00d510035', 1, N'2026-10-27 17:09:19.938', 1, 0, N'2026-10-27 16:59:19.938');
GO
IF NOT EXISTS (SELECT 1 FROM [PasswordResetTokens] WHERE [Id] = 13)
    INSERT INTO [PasswordResetTokens] ([Id], [UserId], [Code], [ResetSessionToken], [IsVerified], [Expiration], [IsUsed], [Attempts], [CreatedAt]) VALUES (13, 6, N'251638', N'30ca761a2824473987e319134dc98b3b', 1, N'2026-11-01 20:14:38.958', 1, 0, N'2026-11-01 20:04:38.959');
GO
IF NOT EXISTS (SELECT 1 FROM [PasswordResetTokens] WHERE [Id] = 14)
    INSERT INTO [PasswordResetTokens] ([Id], [UserId], [Code], [ResetSessionToken], [IsVerified], [Expiration], [IsUsed], [Attempts], [CreatedAt]) VALUES (14, 2, N'621992', N'7a81e78df8f2442b84771bbe11193146', 1, N'2026-11-01 20:15:12.685', 1, 0, N'2026-11-01 20:05:12.685');
GO
IF NOT EXISTS (SELECT 1 FROM [PasswordResetTokens] WHERE [Id] = 15)
    INSERT INTO [PasswordResetTokens] ([Id], [UserId], [Code], [ResetSessionToken], [IsVerified], [Expiration], [IsUsed], [Attempts], [CreatedAt]) VALUES (15, 2, N'822983', N'0f60b0a76dc3415f997f1bf22e9b2995', 1, N'2026-11-01 22:16:32.996', 1, 0, N'2026-11-01 22:06:32.996');
GO
IF NOT EXISTS (SELECT 1 FROM [PasswordResetTokens] WHERE [Id] = 1015)
    INSERT INTO [PasswordResetTokens] ([Id], [UserId], [Code], [ResetSessionToken], [IsVerified], [Expiration], [IsUsed], [Attempts], [CreatedAt]) VALUES (1015, 2, N'856513', NULL, 0, N'2026-11-07 21:57:26.648', 1, 0, N'2026-11-07 21:47:26.650');
GO
IF NOT EXISTS (SELECT 1 FROM [PasswordResetTokens] WHERE [Id] = 1016)
    INSERT INTO [PasswordResetTokens] ([Id], [UserId], [Code], [ResetSessionToken], [IsVerified], [Expiration], [IsUsed], [Attempts], [CreatedAt]) VALUES (1016, 2, N'298885', N'05c44e263b1a466a9ac55be86921a933', 1, N'2026-11-07 22:06:17.486', 1, 0, N'2026-11-07 21:56:17.486');
GO
IF NOT EXISTS (SELECT 1 FROM [PasswordResetTokens] WHERE [Id] = 1017)
    INSERT INTO [PasswordResetTokens] ([Id], [UserId], [Code], [ResetSessionToken], [IsVerified], [Expiration], [IsUsed], [Attempts], [CreatedAt]) VALUES (1017, 2, N'624012', NULL, 0, N'2026-11-07 22:07:21.772', 1, 0, N'2026-11-07 21:57:21.772');
GO
IF NOT EXISTS (SELECT 1 FROM [PasswordResetTokens] WHERE [Id] = 1018)
    INSERT INTO [PasswordResetTokens] ([Id], [UserId], [Code], [ResetSessionToken], [IsVerified], [Expiration], [IsUsed], [Attempts], [CreatedAt]) VALUES (1018, 6, N'397996', N'e41afa26ae3e46bc894dc713f2d77483', 1, N'2026-11-07 22:09:19.953', 0, 0, N'2026-11-07 21:59:19.953');
GO
IF NOT EXISTS (SELECT 1 FROM [PasswordResetTokens] WHERE [Id] = 1019)
    INSERT INTO [PasswordResetTokens] ([Id], [UserId], [Code], [ResetSessionToken], [IsVerified], [Expiration], [IsUsed], [Attempts], [CreatedAt]) VALUES (1019, 2, N'921989', NULL, 0, N'2026-11-07 22:13:47.180', 1, 0, N'2026-11-07 22:03:47.180');
GO
IF NOT EXISTS (SELECT 1 FROM [PasswordResetTokens] WHERE [Id] = 1020)
    INSERT INTO [PasswordResetTokens] ([Id], [UserId], [Code], [ResetSessionToken], [IsVerified], [Expiration], [IsUsed], [Attempts], [CreatedAt]) VALUES (1020, 2, N'934563', N'7ba55e1271be4d9b807a37ec1bae715e', 1, N'2026-11-07 22:17:05.455', 1, 0, N'2026-11-07 22:07:05.455');
GO
SET IDENTITY_INSERT [PasswordResetTokens] OFF;
GO
