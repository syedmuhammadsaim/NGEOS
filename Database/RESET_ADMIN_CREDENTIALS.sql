-- ============================================================================
-- NGEOS MASTER ADMIN CREDENTIALS RESET SCRIPT
-- ============================================================================
-- Run this in MonsterASP.NET Control Panel (Databases > Web Query / Run T-SQL)
-- or in local SQL Server Management Studio (SSMS) to guarantee active login.
--
-- Official Credentials:
--   Email:    admin@ngeos.org
--   Password: NGEOS@Admin123
-- ============================================================================

-- 1. If admin@ngeos.org does not exist, insert it
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Email] = 'admin@ngeos.org')
BEGIN
    INSERT INTO [Users] (
        [FullName], [Email], [PasswordHash], [Phone], [Address],
        [Profession], [Organization], [Role], [ProfileImageUrl],
        [IsActive], [CreatedAt]
    )
    VALUES (
        N'NGEOS Administrator',
        N'admin@ngeos.org',
        N'01a7836dc951c8a141bc82b6d4600c5722d92618cdf56399500989afb3d6d664',
        N'+92 (300) 123-4567',
        N'100 Edhi Street, Saddar, Karachi, Pakistan',
        N'NGO Managing Director',
        N'NGEOS Central Secretariat',
        N'Admin',
        N'/images/ngeos-logo.svg',
        1,
        GETUTCDATE()
    );
    PRINT 'Inserted new admin@ngeos.org administrator account.';
END
ELSE
BEGIN
    -- 2. If exists, update password hash to NGEOS@Admin123, role, and active status
    UPDATE [Users]
    SET [PasswordHash] = '01a7836dc951c8a141bc82b6d4600c5722d92618cdf56399500989afb3d6d664',
        [Role] = 'Admin',
        [IsActive] = 1,
        [FullName] = N'NGEOS Administrator',
        [ProfileImageUrl] = '/images/ngeos-logo.svg'
    WHERE [Email] = 'admin@ngeos.org';
    PRINT 'Successfully updated admin@ngeos.org credentials to active Admin.';
END
GO

-- 3. Also synchronize backup admin account (ngeos@gmail.com)
IF EXISTS (SELECT 1 FROM [Users] WHERE [Email] = 'ngeos@gmail.com')
BEGIN
    UPDATE [Users]
    SET [PasswordHash] = '01a7836dc951c8a141bc82b6d4600c5722d92618cdf56399500989afb3d6d664',
        [Role] = 'Admin',
        [IsActive] = 1,
        [ProfileImageUrl] = '/images/ngeos-logo.svg'
    WHERE [Email] = 'ngeos@gmail.com';
    PRINT 'Synchronized backup admin account (ngeos@gmail.com).';
END
GO

PRINT '================================================================';
PRINT 'SUCCESS: Admin credentials are now verified and active:';
PRINT '  Email:    admin@ngeos.org';
PRINT '  Password: NGEOS@Admin123';
PRINT '================================================================';
GO
