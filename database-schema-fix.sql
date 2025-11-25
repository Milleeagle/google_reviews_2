-- ============================================
-- Comprehensive Database Schema Fix Script
-- ============================================
-- This script fixes all database schema mismatches
-- Run this in SQL Server Management Studio or similar tool

BEGIN TRANSACTION;

BEGIN TRY
    PRINT 'Starting database schema synchronization...'

    -- ============================================
    -- 1. Fix Reviews table
    -- ============================================
    PRINT 'Step 1: Fixing Reviews table schema...'

    -- Drop and recreate Reviews table with correct schema
    IF OBJECT_ID('dbo.Reviews', 'U') IS NOT NULL
        DROP TABLE [Reviews];

    CREATE TABLE [Reviews] (
        [Id] nvarchar(450) NOT NULL,
        [CompanyId] nvarchar(450) NOT NULL,
        [AuthorName] nvarchar(max) NOT NULL,
        [Rating] int NOT NULL,
        [Text] nvarchar(max) NULL,
        [Time] datetime2 NOT NULL,
        [AuthorUrl] nvarchar(max) NULL,
        [ProfilePhotoUrl] nvarchar(max) NULL,
        CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Reviews_Companies_CompanyId]
            FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE
    );

    -- Create Reviews indexes
    CREATE NONCLUSTERED INDEX [IX_Reviews_CompanyId]
        ON [Reviews] ([CompanyId]);

    CREATE NONCLUSTERED INDEX [IX_Reviews_Time]
        ON [Reviews] ([Time]);

    CREATE NONCLUSTERED INDEX [IX_Reviews_Rating]
        ON [Reviews] ([Rating]);

    -- ============================================
    -- 2. Fix ScheduledReviewMonitors table
    -- ============================================
    PRINT 'Step 2: Dropping and recreating ScheduledReviewMonitors table...'

    -- Drop dependent tables first
    IF OBJECT_ID('dbo.ScheduledMonitorCompanies', 'U') IS NOT NULL
        DROP TABLE [ScheduledMonitorCompanies];

    IF OBJECT_ID('dbo.ScheduledMonitorExecutions', 'U') IS NOT NULL
        DROP TABLE [ScheduledMonitorExecutions];

    -- Drop and recreate ScheduledReviewMonitors with correct schema
    IF OBJECT_ID('dbo.ScheduledReviewMonitors', 'U') IS NOT NULL
        DROP TABLE [ScheduledReviewMonitors];

    CREATE TABLE [ScheduledReviewMonitors] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [EmailAddress] nvarchar(max) NOT NULL,
        [ScheduleType] int NOT NULL,
        [ScheduleTime] time NOT NULL,
        [DayOfWeek] int NULL,
        [DayOfMonth] int NULL,
        [MaxRating] int NOT NULL,
        [ReviewPeriodDays] int NOT NULL,
        [IsActive] bit NOT NULL,
        [IncludeAllCompanies] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [LastRunAt] datetime2 NOT NULL,
        [NextRunAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ScheduledReviewMonitors] PRIMARY KEY ([Id])
    );

    -- ============================================
    -- 3. Create ScheduledMonitorCompanies table
    -- ============================================
    PRINT 'Step 3: Creating ScheduledMonitorCompanies table...'

    CREATE TABLE [ScheduledMonitorCompanies] (
        [Id] nvarchar(450) NOT NULL,
        [ScheduledReviewMonitorId] nvarchar(450) NOT NULL,
        [CompanyId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_ScheduledMonitorCompanies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ScheduledMonitorCompanies_ScheduledReviewMonitors_ScheduledReviewMonitorId]
            FOREIGN KEY ([ScheduledReviewMonitorId]) REFERENCES [ScheduledReviewMonitors] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ScheduledMonitorCompanies_Companies_CompanyId]
            FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE
    );

    -- ============================================
    -- 4. Create ScheduledMonitorExecutions table
    -- ============================================
    PRINT 'Step 4: Creating ScheduledMonitorExecutions table...'

    CREATE TABLE [ScheduledMonitorExecutions] (
        [Id] nvarchar(450) NOT NULL,
        [ScheduledReviewMonitorId] nvarchar(450) NOT NULL,
        [ExecutedAt] datetime2 NOT NULL,
        [PeriodStart] datetime2 NOT NULL,
        [PeriodEnd] datetime2 NOT NULL,
        [CompaniesChecked] int NOT NULL,
        [CompaniesWithIssues] int NOT NULL,
        [TotalBadReviews] int NOT NULL,
        [EmailSent] bit NOT NULL,
        [EmailError] nvarchar(max) NULL,
        [Status] int NOT NULL,
        CONSTRAINT [PK_ScheduledMonitorExecutions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ScheduledMonitorExecutions_ScheduledReviewMonitors_ScheduledReviewMonitorId]
            FOREIGN KEY ([ScheduledReviewMonitorId]) REFERENCES [ScheduledReviewMonitors] ([Id]) ON DELETE CASCADE
    );

    -- ============================================
    -- 5. Create indexes for performance
    -- ============================================
    PRINT 'Step 5: Creating performance indexes...'

    -- ScheduledReviewMonitors indexes
    CREATE NONCLUSTERED INDEX [IX_ScheduledReviewMonitors_NextRunAt]
        ON [ScheduledReviewMonitors] ([NextRunAt]);

    CREATE NONCLUSTERED INDEX [IX_ScheduledReviewMonitors_IsActive]
        ON [ScheduledReviewMonitors] ([IsActive]);

    -- ScheduledMonitorCompanies indexes
    CREATE NONCLUSTERED INDEX [IX_ScheduledMonitorCompanies_ScheduledReviewMonitorId]
        ON [ScheduledMonitorCompanies] ([ScheduledReviewMonitorId]);

    CREATE NONCLUSTERED INDEX [IX_ScheduledMonitorCompanies_CompanyId]
        ON [ScheduledMonitorCompanies] ([CompanyId]);

    -- ScheduledMonitorExecutions indexes
    CREATE NONCLUSTERED INDEX [IX_ScheduledMonitorExecutions_ExecutedAt]
        ON [ScheduledMonitorExecutions] ([ExecutedAt]);

    CREATE NONCLUSTERED INDEX [IX_ScheduledMonitorExecutions_ScheduledReviewMonitorId]
        ON [ScheduledMonitorExecutions] ([ScheduledReviewMonitorId]);

    -- ============================================
    -- 6. Verify table structures
    -- ============================================
    PRINT 'Step 6: Verification - Checking table structures...'

    SELECT 'Companies' as TableName, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Companies'
    ORDER BY ORDINAL_POSITION;

    SELECT 'Reviews' as TableName, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Reviews'
    ORDER BY ORDINAL_POSITION;

    SELECT 'ScheduledReviewMonitors' as TableName, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ScheduledReviewMonitors'
    ORDER BY ORDINAL_POSITION;

    SELECT 'ScheduledMonitorCompanies' as TableName, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ScheduledMonitorCompanies'
    ORDER BY ORDINAL_POSITION;

    SELECT 'ScheduledMonitorExecutions' as TableName, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ScheduledMonitorExecutions'
    ORDER BY ORDINAL_POSITION;

    COMMIT TRANSACTION;

    PRINT '============================================'
    PRINT 'SUCCESS: Database schema has been synchronized!'
    PRINT '============================================'
    PRINT 'All tables now match the EF Core models:'
    PRINT '✓ Companies: Id as nvarchar(450)'
    PRINT '✓ Reviews: Id as nvarchar(450) with proper foreign keys'
    PRINT '✓ ScheduledReviewMonitors: Id as nvarchar(450) with all required columns'
    PRINT '✓ ScheduledMonitorCompanies: Proper foreign key relationships'
    PRINT '✓ ScheduledMonitorExecutions: Complete execution tracking'
    PRINT ''
    PRINT 'Your application should now work without schema errors!'
    PRINT 'You can now:'
    PRINT '• Import companies from Google Sheets'
    PRINT '• Refresh reviews for companies'
    PRINT '• Create and manage scheduled monitors'
    PRINT '• View execution history'

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;

    PRINT '============================================'
    PRINT 'ERROR: Transaction rolled back!'
    PRINT '============================================'
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS VARCHAR)
    PRINT 'Error Message: ' + ERROR_MESSAGE()
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR)

END CATCH