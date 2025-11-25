-- SQL Server Database Script (Hosting Compatible)
-- Cleaned for maximum hosting compatibility
-- Generated on: 2025-09-12 18:48:29

-- Simple table creation and data insertion
-- Compatible with most SQL Server hosting providers
CREATE TABLE [__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED 
(
	[MigrationId] ASC
) 
) ;\n
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RoleId] [nvarchar](450) NOT NULL,
	[ClaimType] [nvarchar](max) NULL,
	[ClaimValue] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
) 
)  TEXTIMAGE_;\n
	[Id] [nvarchar](450) NOT NULL,
	[Name] [nvarchar](256) NULL,
	[NormalizedName] [nvarchar](256) NULL,
	[ConcurrencyStamp] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetRoles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
) 
)  TEXTIMAGE_;\n
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [nvarchar](450) NOT NULL,
	[ClaimType] [nvarchar](max) NULL,
	[ClaimValue] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
) 
)  TEXTIMAGE_;\n
	[LoginProvider] [nvarchar](128) NOT NULL,
	[ProviderKey] [nvarchar](128) NOT NULL,
	[ProviderDisplayName] [nvarchar](max) NULL,
	[UserId] [nvarchar](450) NOT NULL,
 CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY CLUSTERED 
(
	[LoginProvider] ASC,
	[ProviderKey] ASC
) 
)  TEXTIMAGE_;\n
	[UserId] [nvarchar](450) NOT NULL,
	[RoleId] [nvarchar](450) NOT NULL,
 CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[RoleId] ASC
) 
) ;\n
	[Id] [nvarchar](450) NOT NULL,
	[UserName] [nvarchar](256) NULL,
	[NormalizedUserName] [nvarchar](256) NULL,
	[Email] [nvarchar](256) NULL,
	[NormalizedEmail] [nvarchar](256) NULL,
	[EmailConfirmed] [bit] NOT NULL,
	[PasswordHash] [nvarchar](max) NULL,
	[SecurityStamp] [nvarchar](max) NULL,
	[ConcurrencyStamp] [nvarchar](max) NULL,
	[PhoneNumber] [nvarchar](max) NULL,
	[PhoneNumberConfirmed] [bit] NOT NULL,
	[TwoFactorEnabled] [bit] NOT NULL,
	[LockoutEnd] [datetimeoffset](7) NULL,
	[LockoutEnabled] [bit] NOT NULL,
	[AccessFailedCount] [int] NOT NULL,
 CONSTRAINT [PK_AspNetUsers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
) 
)  TEXTIMAGE_;\n
	[UserId] [nvarchar](450) NOT NULL,
	[LoginProvider] [nvarchar](128) NOT NULL,
	[Name] [nvarchar](128) NOT NULL,
	[Value] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[LoginProvider] ASC,
	[Name] ASC
) 
)  TEXTIMAGE_;\n
	[Id] [nvarchar](450) NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
	[PlaceId] [nvarchar](450) NULL,
	[GoogleMapsUrl] [nvarchar](max) NULL,
	[IsActive] [bit] NOT NULL,
	[LastUpdated] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Companies] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
) 
)  TEXTIMAGE_;\n
	[Id] [nvarchar](450) NOT NULL,
	[CompanyId] [nvarchar](450) NOT NULL,
	[AuthorName] [nvarchar](max) NOT NULL,
	[Rating] [int] NOT NULL,
	[Text] [nvarchar](max) NULL,
	[Time] [datetime2](7) NOT NULL,
	[AuthorUrl] [nvarchar](max) NULL,
	[ProfilePhotoUrl] [nvarchar](max) NULL,
 CONSTRAINT [PK_Reviews] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
) 
)  TEXTIMAGE_;\n
	[Id] [nvarchar](450) NOT NULL,
	[ScheduledReviewMonitorId] [nvarchar](450) NOT NULL,
	[CompanyId] [nvarchar](450) NOT NULL,
 CONSTRAINT [PK_ScheduledMonitorCompanies] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
) 
) ;\n
	[Id] [nvarchar](450) NOT NULL,
	[ScheduledReviewMonitorId] [nvarchar](450) NOT NULL,
	[ExecutedAt] [datetime2](7) NOT NULL,
	[PeriodStart] [datetime2](7) NOT NULL,
	[PeriodEnd] [datetime2](7) NOT NULL,
	[CompaniesChecked] [int] NOT NULL,
	[CompaniesWithIssues] [int] NOT NULL,
	[TotalBadReviews] [int] NOT NULL,
	[EmailSent] [bit] NOT NULL,
	[EmailError] [nvarchar](max) NULL,
	[Status] [int] NOT NULL,
 CONSTRAINT [PK_ScheduledMonitorExecutions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
) 
)  TEXTIMAGE_;\n
	[Id] [nvarchar](450) NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[EmailAddress] [nvarchar](max) NOT NULL,
	[ScheduleType] [int] NOT NULL,
	[ScheduleTime] [time](7) NOT NULL,
	[DayOfWeek] [int] NULL,
	[DayOfMonth] [int] NULL,
	[MaxRating] [int] NOT NULL,
	[ReviewPeriodDays] [int] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[IncludeAllCompanies] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[LastRunAt] [datetime2](7) NOT NULL,
	[NextRunAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_ScheduledReviewMonitors] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
) 
)  TEXTIMAGE_;\nSET ANSI_PADDING ON;\n
CREATE NONCLUSTERED INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims]
(
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ;\nSET ANSI_PADDING ON;\n
CREATE UNIQUE NONCLUSTERED INDEX [RoleNameIndex] ON [AspNetRoles]
(
	[NormalizedName] ASC
)
WHERE ([NormalizedName] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ;\nSET ANSI_PADDING ON;\n
CREATE NONCLUSTERED INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ;\nSET ANSI_PADDING ON;\n
CREATE NONCLUSTERED INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ;\nSET ANSI_PADDING ON;\n
CREATE NONCLUSTERED INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles]
(
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ;\nSET ANSI_PADDING ON;\n
CREATE NONCLUSTERED INDEX [EmailIndex] ON [AspNetUsers]
(
	[NormalizedEmail] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ;\nSET ANSI_PADDING ON;\n
CREATE UNIQUE NONCLUSTERED INDEX [UserNameIndex] ON [AspNetUsers]
(
	[NormalizedUserName] ASC
)
WHERE ([NormalizedUserName] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ;\nSET ANSI_PADDING ON;\n
CREATE UNIQUE NONCLUSTERED INDEX [IX_Companies_PlaceId] ON [Companies]
(
	[PlaceId] ASC
)
WHERE ([PlaceId] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ;\nSET ANSI_PADDING ON;\n
CREATE NONCLUSTERED INDEX [IX_Reviews_CompanyId] ON [Reviews]
(
	[CompanyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ;\n
CREATE NONCLUSTERED INDEX [IX_Reviews_Time] ON [Reviews]
(
	[Time] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ;\nSET ANSI_PADDING ON;\n
CREATE NONCLUSTERED INDEX [IX_ScheduledMonitorCompanies_CompanyId] ON [ScheduledMonitorCompanies]
(
	[CompanyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ;\nSET ANSI_PADDING ON;\n
CREATE NONCLUSTERED INDEX [IX_ScheduledMonitorCompanies_ScheduledReviewMonitorId] ON [ScheduledMonitorCompanies]
(
	[ScheduledReviewMonitorId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ;\n
CREATE NONCLUSTERED INDEX [IX_ScheduledMonitorExecutions_ExecutedAt] ON [ScheduledMonitorExecutions]
(
	[ExecutedAt] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ;\nSET ANSI_PADDING ON;\n
CREATE NONCLUSTERED INDEX [IX_ScheduledMonitorExecutions_ScheduledReviewMonitorId] ON [ScheduledMonitorExecutions]
(
	[ScheduledReviewMonitorId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ;\n
CREATE NONCLUSTERED INDEX [IX_ScheduledReviewMonitors_IsActive] ON [ScheduledReviewMonitors]
(
	[IsActive] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ;\n
CREATE NONCLUSTERED INDEX [IX_ScheduledReviewMonitors_NextRunAt] ON [ScheduledReviewMonitors]
(
	[NextRunAt] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ;\nALTER TABLE [AspNetRoleClaims]  WITH CHECK ADD  CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [AspNetRoles] ([Id])
ON DELETE CASCADE;\nALTER TABLE [AspNetRoleClaims] CHECK CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId];\nALTER TABLE [AspNetUserClaims]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [AspNetUsers] ([Id])
ON DELETE CASCADE;\nALTER TABLE [AspNetUserClaims] CHECK CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId];\nALTER TABLE [AspNetUserLogins]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [AspNetUsers] ([Id])
ON DELETE CASCADE;\nALTER TABLE [AspNetUserLogins] CHECK CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId];\nALTER TABLE [AspNetUserRoles]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [AspNetRoles] ([Id])
ON DELETE CASCADE;\nALTER TABLE [AspNetUserRoles] CHECK CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId];\nALTER TABLE [AspNetUserRoles]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [AspNetUsers] ([Id])
ON DELETE CASCADE;\nALTER TABLE [AspNetUserRoles] CHECK CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId];\nALTER TABLE [AspNetUserTokens]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [AspNetUsers] ([Id])
ON DELETE CASCADE;\nALTER TABLE [AspNetUserTokens] CHECK CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId];\nALTER TABLE [Reviews]  WITH CHECK ADD  CONSTRAINT [FK_Reviews_Companies_CompanyId] FOREIGN KEY([CompanyId])
REFERENCES [Companies] ([Id])
ON DELETE CASCADE;\nALTER TABLE [Reviews] CHECK CONSTRAINT [FK_Reviews_Companies_CompanyId];\nALTER TABLE [ScheduledMonitorCompanies]  WITH CHECK ADD  CONSTRAINT [FK_ScheduledMonitorCompanies_Companies_CompanyId] FOREIGN KEY([CompanyId])
REFERENCES [Companies] ([Id])
ON DELETE CASCADE;\nALTER TABLE [ScheduledMonitorCompanies] CHECK CONSTRAINT [FK_ScheduledMonitorCompanies_Companies_CompanyId];\nALTER TABLE [ScheduledMonitorCompanies]  WITH CHECK ADD  CONSTRAINT [FK_ScheduledMonitorCompanies_ScheduledReviewMonitors_ScheduledReviewMonitorId] FOREIGN KEY([ScheduledReviewMonitorId])
REFERENCES [ScheduledReviewMonitors] ([Id])
ON DELETE CASCADE;\nALTER TABLE [ScheduledMonitorCompanies] CHECK CONSTRAINT [FK_ScheduledMonitorCompanies_ScheduledReviewMonitors_ScheduledReviewMonitorId];\nALTER TABLE [ScheduledMonitorExecutions]  WITH CHECK ADD  CONSTRAINT [FK_ScheduledMonitorExecutions_ScheduledReviewMonitors_ScheduledReviewMonitorId] FOREIGN KEY([ScheduledReviewMonitorId])
REFERENCES [ScheduledReviewMonitors] ([Id])
ON DELETE CASCADE;\nALTER TABLE [ScheduledMonitorExecutions] CHECK CONSTRAINT [FK_ScheduledMonitorExecutions_ScheduledReviewMonitors_ScheduledReviewMonitorId];\n
-- Import completed
-- Remember to update your connection strings