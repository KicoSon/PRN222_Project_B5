IF DB_ID(N'StudentPartTimeJobDB') IS NULL
BEGIN
    CREATE DATABASE StudentPartTimeJobDB;
END
GO

USE StudentPartTimeJobDB;
GO

/* ============================================================
   DROP TABLES IF EXIST
   ============================================================ */

IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NOT NULL DROP TABLE dbo.AuditLogs;
IF OBJECT_ID(N'dbo.Notifications', N'U') IS NOT NULL DROP TABLE dbo.Notifications;
IF OBJECT_ID(N'dbo.Bookmarks', N'U') IS NOT NULL DROP TABLE dbo.Bookmarks;
IF OBJECT_ID(N'dbo.Applications', N'U') IS NOT NULL DROP TABLE dbo.Applications;
IF OBJECT_ID(N'dbo.Jobs', N'U') IS NOT NULL DROP TABLE dbo.Jobs;
IF OBJECT_ID(N'dbo.Resumes', N'U') IS NOT NULL DROP TABLE dbo.Resumes;
IF OBJECT_ID(N'dbo.Students', N'U') IS NOT NULL DROP TABLE dbo.Students;
IF OBJECT_ID(N'dbo.Employers', N'U') IS NOT NULL DROP TABLE dbo.Employers;
IF OBJECT_ID(N'dbo.Companies', N'U') IS NOT NULL DROP TABLE dbo.Companies;
IF OBJECT_ID(N'dbo.JobTypes', N'U') IS NOT NULL DROP TABLE dbo.JobTypes;
IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID(N'dbo.Provinces', N'U') IS NOT NULL DROP TABLE dbo.Provinces;
IF OBJECT_ID(N'dbo.UserRoles', N'U') IS NOT NULL DROP TABLE dbo.UserRoles;
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID(N'dbo.Roles', N'U') IS NOT NULL DROP TABLE dbo.Roles;
GO

/* ============================================================
   ACCOUNT TABLES
   ============================================================ */

CREATE TABLE dbo.Roles
(
    RoleId      INT IDENTITY(1,1) NOT NULL,
    RoleName    NVARCHAR(50) NOT NULL,
    Description NVARCHAR(255) NULL,
    CreatedAt   DATETIME2(0) NOT NULL CONSTRAINT DF_Roles_CreatedAt DEFAULT SYSDATETIME(),

    CONSTRAINT PK_Roles PRIMARY KEY (RoleId),
    CONSTRAINT UQ_Roles_RoleName UNIQUE (RoleName),
    CONSTRAINT CK_Roles_RoleName CHECK (RoleName IN (N'Admin', N'Student', N'Employer'))
);
GO

CREATE TABLE dbo.Users
(
    UserId        INT IDENTITY(1,1) NOT NULL,
    FullName      NVARCHAR(100) NOT NULL,
    Email         NVARCHAR(150) NOT NULL,
    PasswordHash  NVARCHAR(500) NOT NULL,
    PhoneNumber   NVARCHAR(20) NULL,
    AvatarUrl     NVARCHAR(500) NULL,
    Gender        NVARCHAR(20) NULL,
    DateOfBirth   DATE NULL,
    Address       NVARCHAR(255) NULL,
    IsActive      BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    CreatedAt     DATETIME2(0) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt     DATETIME2(0) NULL,

    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Gender CHECK (Gender IS NULL OR Gender IN (N'Male', N'Female', N'Other'))
);
GO

CREATE TABLE dbo.UserRoles
(
    UserId INT NOT NULL,
    RoleId INT NOT NULL,

    CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId)
);
GO

/* ============================================================
   MASTER TABLES
   ============================================================ */

CREATE TABLE dbo.Provinces
(
    ProvinceId      INT IDENTITY(1,1) NOT NULL,
    ProvinceName    NVARCHAR(100) NOT NULL,
    CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_Provinces_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt       DATETIME2(0) NULL,

    CONSTRAINT PK_Provinces PRIMARY KEY (ProvinceId),
    CONSTRAINT UQ_Provinces_ProvinceName UNIQUE (ProvinceName)
);
GO

CREATE TABLE dbo.Categories
(
    CategoryId      INT IDENTITY(1,1) NOT NULL,
    CategoryName    NVARCHAR(100) NOT NULL,
    Description     NVARCHAR(500) NULL,
    IsActive        BIT NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT 1,
    CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt       DATETIME2(0) NULL,

    CONSTRAINT PK_Categories PRIMARY KEY (CategoryId),
    CONSTRAINT UQ_Categories_CategoryName UNIQUE (CategoryName)
);
GO

CREATE TABLE dbo.JobTypes
(
    JobTypeId       INT IDENTITY(1,1) NOT NULL,
    TypeName        NVARCHAR(100) NOT NULL,
    Description     NVARCHAR(500) NULL,
    IsActive        BIT NOT NULL CONSTRAINT DF_JobTypes_IsActive DEFAULT 1,
    CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_JobTypes_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt       DATETIME2(0) NULL,

    CONSTRAINT PK_JobTypes PRIMARY KEY (JobTypeId),
    CONSTRAINT UQ_JobTypes_TypeName UNIQUE (TypeName)
);
GO

/* ============================================================
   COMPANIES
   ============================================================ */

CREATE TABLE dbo.Companies
(
    CompanyId       INT IDENTITY(1,1) NOT NULL,
    CompanyName     NVARCHAR(200) NOT NULL,
    LogoUrl         NVARCHAR(500) NULL,
    Website         NVARCHAR(255) NULL,
    Email           NVARCHAR(150) NULL,
    Phone           NVARCHAR(20) NULL,
    Description     NVARCHAR(MAX) NULL,
    Address         NVARCHAR(255) NULL,
    ProvinceId      INT NULL,
    Status          NVARCHAR(30) NOT NULL CONSTRAINT DF_Companies_Status DEFAULT N'Active',
    CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_Companies_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt       DATETIME2(0) NULL,

    CONSTRAINT PK_Companies PRIMARY KEY (CompanyId),
    CONSTRAINT FK_Companies_Provinces FOREIGN KEY (ProvinceId) REFERENCES dbo.Provinces(ProvinceId),
    CONSTRAINT CK_Companies_Status CHECK (Status IN (N'Active', N'Inactive', N'Pending', N'Blocked'))
);
GO

/* ============================================================
   USER PROFILE TABLES
   ============================================================ */

CREATE TABLE dbo.Students
(
    StudentId       INT IDENTITY(1,1) NOT NULL,
    UserId          INT NOT NULL,
    University      NVARCHAR(200) NULL,
    Major           NVARCHAR(150) NULL,
    GraduationYear  INT NULL,
    Experience      NVARCHAR(MAX) NULL,
    SkillSummary    NVARCHAR(MAX) NULL,
    CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_Students_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt       DATETIME2(0) NULL,

    CONSTRAINT PK_Students PRIMARY KEY (StudentId),
    CONSTRAINT UQ_Students_UserId UNIQUE (UserId),
    CONSTRAINT FK_Students_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Students_GraduationYear CHECK (GraduationYear IS NULL OR GraduationYear BETWEEN 1990 AND 2100)
);
GO

CREATE TABLE dbo.Employers
(
    EmployerId      INT IDENTITY(1,1) NOT NULL,
    UserId          INT NOT NULL,
    CompanyId       INT NOT NULL,
    Position        NVARCHAR(100) NULL,
    CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_Employers_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt       DATETIME2(0) NULL,

    CONSTRAINT PK_Employers PRIMARY KEY (EmployerId),
    CONSTRAINT UQ_Employers_UserId UNIQUE (UserId),
    CONSTRAINT FK_Employers_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Employers_Companies FOREIGN KEY (CompanyId) REFERENCES dbo.Companies(CompanyId)
);
GO

/* ============================================================
   RESUMES / CV
   ============================================================ */

CREATE TABLE dbo.Resumes
(
    ResumeId        INT IDENTITY(1,1) NOT NULL,
    StudentId       INT NOT NULL,
    FileName        NVARCHAR(255) NOT NULL,
    FilePath        NVARCHAR(500) NOT NULL,
    ContentType     NVARCHAR(100) NULL,
    FileSize        BIGINT NULL,
    IsDefault       BIT NOT NULL CONSTRAINT DF_Resumes_IsDefault DEFAULT 0,
    UploadedAt      DATETIME2(0) NOT NULL CONSTRAINT DF_Resumes_UploadedAt DEFAULT SYSDATETIME(),

    CONSTRAINT PK_Resumes PRIMARY KEY (ResumeId),
    CONSTRAINT FK_Resumes_Students FOREIGN KEY (StudentId) REFERENCES dbo.Students(StudentId),
    CONSTRAINT CK_Resumes_FileSize CHECK (FileSize IS NULL OR FileSize > 0)
);
GO

/* ============================================================
   JOB POSTS
   ============================================================ */

CREATE TABLE dbo.Jobs
(
    JobId           INT IDENTITY(1,1) NOT NULL,
    EmployerId      INT NOT NULL,
    CategoryId      INT NOT NULL,
    JobTypeId       INT NOT NULL,
    ProvinceId      INT NOT NULL,

    Title           NVARCHAR(200) NOT NULL,
    Description     NVARCHAR(MAX) NOT NULL,
    Requirement     NVARCHAR(MAX) NULL,
    Benefit         NVARCHAR(MAX) NULL,

    SalaryMin       DECIMAL(18,2) NULL,
    SalaryMax       DECIMAL(18,2) NULL,
    Quantity        INT NOT NULL CONSTRAINT DF_Jobs_Quantity DEFAULT 1,

    WorkingTime     NVARCHAR(100) NULL,
    Address         NVARCHAR(255) NULL,
    Deadline        DATE NOT NULL,

    Status          NVARCHAR(30) NOT NULL CONSTRAINT DF_Jobs_Status DEFAULT N'Pending',
    RejectReason    NVARCHAR(500) NULL,

    ApprovedBy      INT NULL,
    ApprovedAt      DATETIME2(0) NULL,

    CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_Jobs_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt       DATETIME2(0) NULL,

    CONSTRAINT PK_Jobs PRIMARY KEY (JobId),

    CONSTRAINT FK_Jobs_Employers FOREIGN KEY (EmployerId) REFERENCES dbo.Employers(EmployerId),
    CONSTRAINT FK_Jobs_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(CategoryId),
    CONSTRAINT FK_Jobs_JobTypes FOREIGN KEY (JobTypeId) REFERENCES dbo.JobTypes(JobTypeId),
    CONSTRAINT FK_Jobs_Provinces FOREIGN KEY (ProvinceId) REFERENCES dbo.Provinces(ProvinceId),
    CONSTRAINT FK_Jobs_ApprovedBy_Users FOREIGN KEY (ApprovedBy) REFERENCES dbo.Users(UserId),

    CONSTRAINT CK_Jobs_Status CHECK (Status IN (N'Pending', N'Approved', N'Rejected', N'Closed', N'Expired')),
    CONSTRAINT CK_Jobs_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_Jobs_Salary CHECK (
        (SalaryMin IS NULL AND SalaryMax IS NULL)
        OR (SalaryMin IS NOT NULL AND SalaryMax IS NULL AND SalaryMin >= 0)
        OR (SalaryMin IS NULL AND SalaryMax IS NOT NULL AND SalaryMax >= 0)
        OR (SalaryMin IS NOT NULL AND SalaryMax IS NOT NULL AND SalaryMin >= 0 AND SalaryMax >= SalaryMin)
    )
);
GO

/* ============================================================
   APPLICATIONS
   ============================================================ */

CREATE TABLE dbo.Applications
(
    ApplicationId   INT IDENTITY(1,1) NOT NULL,
    StudentId       INT NOT NULL,
    JobId           INT NOT NULL,
    ResumeId        INT NULL,

    CoverLetter     NVARCHAR(MAX) NULL,
    Status          NVARCHAR(30) NOT NULL CONSTRAINT DF_Applications_Status DEFAULT N'Pending',
    EmployerNote    NVARCHAR(MAX) NULL,

    AppliedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_Applications_AppliedAt DEFAULT SYSDATETIME(),
    UpdatedAt       DATETIME2(0) NULL,

    CONSTRAINT PK_Applications PRIMARY KEY (ApplicationId),

    CONSTRAINT FK_Applications_Students FOREIGN KEY (StudentId) REFERENCES dbo.Students(StudentId),
    CONSTRAINT FK_Applications_Jobs FOREIGN KEY (JobId) REFERENCES dbo.Jobs(JobId),
    CONSTRAINT FK_Applications_Resumes FOREIGN KEY (ResumeId) REFERENCES dbo.Resumes(ResumeId),

    CONSTRAINT UQ_Applications_Student_Job UNIQUE (StudentId, JobId),
    CONSTRAINT CK_Applications_Status CHECK (Status IN (N'Pending', N'Reviewed', N'Interview', N'Approved', N'Rejected', N'Cancelled'))
);
GO

/* ============================================================
   BOOKMARKS
   ============================================================ */

CREATE TABLE dbo.Bookmarks
(
    BookmarkId      INT IDENTITY(1,1) NOT NULL,
    StudentId       INT NOT NULL,
    JobId           INT NOT NULL,
    CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_Bookmarks_CreatedAt DEFAULT SYSDATETIME(),

    CONSTRAINT PK_Bookmarks PRIMARY KEY (BookmarkId),

    CONSTRAINT FK_Bookmarks_Students FOREIGN KEY (StudentId) REFERENCES dbo.Students(StudentId),
    CONSTRAINT FK_Bookmarks_Jobs FOREIGN KEY (JobId) REFERENCES dbo.Jobs(JobId),

    CONSTRAINT UQ_Bookmarks_Student_Job UNIQUE (StudentId, JobId)
);
GO

/* ============================================================
   NOTIFICATIONS
   ============================================================ */

CREATE TABLE dbo.Notifications
(
    NotificationId  INT IDENTITY(1,1) NOT NULL,
    UserId          INT NOT NULL,
    Title           NVARCHAR(200) NOT NULL,
    Content         NVARCHAR(MAX) NULL,
    Type            NVARCHAR(50) NOT NULL CONSTRAINT DF_Notifications_Type DEFAULT N'System',
    IsRead          BIT NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT 0,
    CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT SYSDATETIME(),

    CONSTRAINT PK_Notifications PRIMARY KEY (NotificationId),
    CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Notifications_Type CHECK (Type IN (N'System', N'Job', N'Application', N'Account'))
);
GO

/* ============================================================
   AUDIT LOGS
   No IPAddress field because this is a personal/student project.
   ============================================================ */

CREATE TABLE dbo.AuditLogs
(
    LogId           INT IDENTITY(1,1) NOT NULL,
    UserId          INT NULL,
    Action          NVARCHAR(100) NOT NULL,
    EntityName      NVARCHAR(100) NOT NULL,
    EntityId        INT NULL,
    Description     NVARCHAR(500) NULL,
    CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT SYSDATETIME(),

    CONSTRAINT PK_AuditLogs PRIMARY KEY (LogId),
    CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
);
GO

/* ============================================================
   INDEXES
   ============================================================ */

CREATE INDEX IX_UserRoles_RoleId ON dbo.UserRoles(RoleId);

CREATE INDEX IX_Companies_ProvinceId ON dbo.Companies(ProvinceId);

CREATE INDEX IX_Students_UserId ON dbo.Students(UserId);
CREATE INDEX IX_Employers_UserId ON dbo.Employers(UserId);
CREATE INDEX IX_Employers_CompanyId ON dbo.Employers(CompanyId);

CREATE INDEX IX_Resumes_StudentId ON dbo.Resumes(StudentId);

CREATE INDEX IX_Jobs_EmployerId ON dbo.Jobs(EmployerId);
CREATE INDEX IX_Jobs_CategoryId ON dbo.Jobs(CategoryId);
CREATE INDEX IX_Jobs_JobTypeId ON dbo.Jobs(JobTypeId);
CREATE INDEX IX_Jobs_ProvinceId ON dbo.Jobs(ProvinceId);
CREATE INDEX IX_Jobs_Status ON dbo.Jobs(Status);
CREATE INDEX IX_Jobs_Deadline ON dbo.Jobs(Deadline);
CREATE INDEX IX_Jobs_Search ON dbo.Jobs(CategoryId, JobTypeId, ProvinceId, Status);

CREATE INDEX IX_Applications_StudentId ON dbo.Applications(StudentId);
CREATE INDEX IX_Applications_JobId ON dbo.Applications(JobId);
CREATE INDEX IX_Applications_Status ON dbo.Applications(Status);

CREATE INDEX IX_Bookmarks_StudentId ON dbo.Bookmarks(StudentId);
CREATE INDEX IX_Bookmarks_JobId ON dbo.Bookmarks(JobId);

CREATE INDEX IX_Notifications_UserId_IsRead ON dbo.Notifications(UserId, IsRead);
CREATE INDEX IX_AuditLogs_UserId ON dbo.AuditLogs(UserId);
CREATE INDEX IX_AuditLogs_Entity ON dbo.AuditLogs(EntityName, EntityId);
GO
