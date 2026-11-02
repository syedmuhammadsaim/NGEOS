IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE TABLE [AboutPages] (
        [Id] int NOT NULL IDENTITY,
        [Slug] nvarchar(60) NOT NULL,
        [Title] nvarchar(150) NOT NULL,
        [Subtitle] nvarchar(250) NULL,
        [ContentHtml] nvarchar(max) NOT NULL,
        [BannerImageUrl] nvarchar(250) NULL,
        [FeaturedImageUrl] nvarchar(250) NULL,
        [LastUpdated] datetime2 NOT NULL,
        CONSTRAINT [PK_AboutPages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE TABLE [ContactMessages] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [Phone] nvarchar(25) NULL,
        [Subject] nvarchar(200) NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [IsRead] bit NOT NULL,
        [ReplyNotes] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ContactMessages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE TABLE [Events] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(150) NOT NULL,
        [ShortDescription] nvarchar(350) NOT NULL,
        [FullDescription] nvarchar(max) NOT NULL,
        [EventDate] datetime2 NOT NULL,
        [Location] nvarchar(150) NOT NULL,
        [ImageUrl] nvarchar(250) NOT NULL,
        [Organizer] nvarchar(100) NOT NULL,
        [IsUpcoming] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Events] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE TABLE [FAQs] (
        [Id] int NOT NULL IDENTITY,
        [Question] nvarchar(250) NOT NULL,
        [Answer] nvarchar(max) NOT NULL,
        [Category] nvarchar(100) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_FAQs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE TABLE [GalleryItems] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(150) NOT NULL,
        [Category] nvarchar(50) NOT NULL,
        [ImageUrl] nvarchar(250) NOT NULL,
        [Description] nvarchar(350) NULL,
        [EventDate] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_GalleryItems] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE TABLE [JobOpportunities] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(150) NOT NULL,
        [Department] nvarchar(100) NOT NULL,
        [JobType] nvarchar(50) NOT NULL,
        [Location] nvarchar(100) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Requirements] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [PostedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_JobOpportunities] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE TABLE [NGOs] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(150) NOT NULL,
        [RegistrationNumber] nvarchar(50) NOT NULL,
        [ShortDescription] nvarchar(300) NOT NULL,
        [FullDescription] nvarchar(max) NOT NULL,
        [LogoUrl] nvarchar(250) NOT NULL,
        [BannerUrl] nvarchar(250) NULL,
        [FocusArea] nvarchar(100) NOT NULL,
        [Website] nvarchar(200) NULL,
        [Email] nvarchar(150) NOT NULL,
        [Phone] nvarchar(30) NOT NULL,
        [Address] nvarchar(250) NULL,
        [EstablishedYear] int NOT NULL,
        [IsVerified] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_NGOs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE TABLE [Partners] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(150) NOT NULL,
        [Category] nvarchar(100) NOT NULL,
        [LogoUrl] nvarchar(250) NOT NULL,
        [WebsiteUrl] nvarchar(200) NULL,
        [Description] nvarchar(350) NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Partners] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE TABLE [TeamMembers] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Role] nvarchar(100) NOT NULL,
        [Bio] nvarchar(350) NOT NULL,
        [PhotoUrl] nvarchar(250) NOT NULL,
        [Email] nvarchar(200) NULL,
        [LinkedInUrl] nvarchar(200) NULL,
        [TwitterUrl] nvarchar(200) NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_TeamMembers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [FullName] nvarchar(100) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [Phone] nvarchar(25) NULL,
        [Address] nvarchar(250) NULL,
        [Profession] nvarchar(100) NULL,
        [Organization] nvarchar(150) NULL,
        [Role] nvarchar(20) NOT NULL,
        [ProfileImageUrl] nvarchar(250) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE TABLE [Causes] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(150) NOT NULL,
        [ShortDescription] nvarchar(350) NOT NULL,
        [FullDescription] nvarchar(max) NOT NULL,
        [Category] nvarchar(50) NOT NULL,
        [TargetAmount] decimal(18,2) NOT NULL,
        [RaisedAmount] decimal(18,2) NOT NULL,
        [ImageUrl] nvarchar(300) NOT NULL,
        [IsFeatured] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [NgoId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Causes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Causes_NGOs_NgoId] FOREIGN KEY ([NgoId]) REFERENCES [NGOs] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE TABLE [Programmes] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(150) NOT NULL,
        [NgoId] int NULL,
        [Category] nvarchar(100) NOT NULL,
        [ShortDescription] nvarchar(350) NOT NULL,
        [FullDescription] nvarchar(max) NOT NULL,
        [ImageUrl] nvarchar(250) NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [Location] nvarchar(150) NOT NULL,
        [TargetBeneficiaries] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Programmes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Programmes_NGOs_NgoId] FOREIGN KEY ([NgoId]) REFERENCES [NGOs] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE TABLE [UserQueries] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NULL,
        [FullName] nvarchar(100) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [Subject] nvarchar(200) NOT NULL,
        [Category] nvarchar(100) NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [AdminReply] nvarchar(max) NULL,
        [Status] nvarchar(30) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RepliedAt] datetime2 NULL,
        CONSTRAINT [PK_UserQueries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserQueries_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE TABLE [Donations] (
        [Id] int NOT NULL IDENTITY,
        [TransactionId] nvarchar(50) NOT NULL,
        [CauseId] int NOT NULL,
        [UserId] int NULL,
        [DonorName] nvarchar(100) NOT NULL,
        [DonorEmail] nvarchar(150) NOT NULL,
        [DonorPhone] nvarchar(25) NULL,
        [Amount] decimal(18,2) NOT NULL,
        [CardType] nvarchar(30) NOT NULL,
        [CardHolderName] nvarchar(100) NOT NULL,
        [MaskedCardNumber] nvarchar(30) NOT NULL,
        [PaymentStatus] nvarchar(30) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [DonationDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Donations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Donations_Causes_CauseId] FOREIGN KEY ([CauseId]) REFERENCES [Causes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Donations_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE TABLE [FriendInvitations] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NULL,
        [SenderName] nvarchar(100) NOT NULL,
        [SenderEmail] nvarchar(150) NOT NULL,
        [FriendEmail] nvarchar(150) NOT NULL,
        [CauseId] int NULL,
        [PersonalMessage] nvarchar(600) NULL,
        [SentAt] datetime2 NOT NULL,
        CONSTRAINT [PK_FriendInvitations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FriendInvitations_Causes_CauseId] FOREIGN KEY ([CauseId]) REFERENCES [Causes] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_FriendInvitations_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE TABLE [ProgrammeInterests] (
        [Id] int NOT NULL IDENTITY,
        [ProgrammeId] int NOT NULL,
        [UserId] int NULL,
        [FullName] nvarchar(100) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [Phone] nvarchar(25) NOT NULL,
        [Occupation] nvarchar(100) NULL,
        [InterestType] nvarchar(50) NOT NULL,
        [Message] nvarchar(500) NULL,
        [Status] nvarchar(30) NOT NULL,
        [SubmittedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ProgrammeInterests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProgrammeInterests_Programmes_ProgrammeId] FOREIGN KEY ([ProgrammeId]) REFERENCES [Programmes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProgrammeInterests_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AboutPages_Slug] ON [AboutPages] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Causes_NgoId] ON [Causes] ([NgoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Donations_CauseId] ON [Donations] ([CauseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Donations_UserId] ON [Donations] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_FriendInvitations_CauseId] ON [FriendInvitations] ([CauseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_FriendInvitations_UserId] ON [FriendInvitations] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProgrammeInterests_ProgrammeId] ON [ProgrammeInterests] ([ProgrammeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProgrammeInterests_UserId] ON [ProgrammeInterests] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Programmes_NgoId] ON [Programmes] ([NgoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserQueries_UserId] ON [UserQueries] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827080324_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260827080324_InitialCreate', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025091132_AddPasswordResetTokens'
)
BEGIN
    CREATE TABLE [PasswordResetTokens] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Code] nvarchar(6) NOT NULL,
        [ResetSessionToken] nvarchar(100) NULL,
        [IsVerified] bit NOT NULL,
        [Expiration] datetime2 NOT NULL,
        [IsUsed] bit NOT NULL,
        [Attempts] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PasswordResetTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025091132_AddPasswordResetTokens'
)
BEGIN
    CREATE INDEX [IX_PasswordResetTokens_Code] ON [PasswordResetTokens] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025091132_AddPasswordResetTokens'
)
BEGIN
    CREATE INDEX [IX_PasswordResetTokens_ResetSessionToken] ON [PasswordResetTokens] ([ResetSessionToken]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025091132_AddPasswordResetTokens'
)
BEGIN
    CREATE INDEX [IX_PasswordResetTokens_UserId] ON [PasswordResetTokens] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025091132_AddPasswordResetTokens'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261025091132_AddPasswordResetTokens', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025102850_AddGoogleIdToUser'
)
BEGIN
    ALTER TABLE [Users] ADD [GoogleId] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025102850_AddGoogleIdToUser'
)
BEGIN
    CREATE INDEX [IX_Users_GoogleId] ON [Users] ([GoogleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025102850_AddGoogleIdToUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261025102850_AddGoogleIdToUser', N'10.0.11');
END;

COMMIT;
GO

