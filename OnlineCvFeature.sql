/* ============================================================================
   FEATURE: ONLINE-CV (v1.0) - Table creation & ALTER
   ----------------------------------------------------------------------------
   IMPORTANT: run ONCE inside the EXISTING database StudentPartTimeJobDB.
   - It does NOT create a new database.
   - It only ADDS 3 new tables and ALTERs dbo.Resumes by adding NULL / DEFAULT
     columns. Existing data and the legacy file-upload CV flow are untouched.
   ============================================================================ */

-- 1) Skills (shared catalog for both online CVs and job posts)
IF OBJECT_ID(N'dbo.Skills', N'U') IS NOT NULL DROP TABLE dbo.Skills;
CREATE TABLE dbo.Skills
(
    SkillId    INT IDENTITY(1,1) NOT NULL,
    SkillName  NVARCHAR(100) NOT NULL,
    IsActive   BIT NOT NULL DEFAULT 1,
    CreatedAt  DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Skills PRIMARY KEY (SkillId),
    CONSTRAINT UQ_Skills_SkillName UNIQUE (SkillName)
);
GO

-- 2) Join: one online CV -> many skills
IF OBJECT_ID(N'dbo.ResumeSkills', N'U') IS NOT NULL DROP TABLE dbo.ResumeSkills;
CREATE TABLE dbo.ResumeSkills
(
    ResumeId INT NOT NULL,
    SkillId  INT NOT NULL,
    CONSTRAINT PK_ResumeSkills PRIMARY KEY (ResumeId, SkillId),
    CONSTRAINT FK_ResumeSkills_Resumes FOREIGN KEY (ResumeId) REFERENCES dbo.Resumes(ResumeId),
    CONSTRAINT FK_ResumeSkills_Skills  FOREIGN KEY (SkillId)  REFERENCES dbo.Skills(SkillId)
);
GO

-- 3) Join: one job post requires many skills
IF OBJECT_ID(N'dbo.JobSkills', N'U') IS NOT NULL DROP TABLE dbo.JobSkills;
CREATE TABLE dbo.JobSkills
(
    JobId   INT NOT NULL,
    SkillId INT NOT NULL,
    CONSTRAINT PK_JobSkills PRIMARY KEY (JobId, SkillId),
    CONSTRAINT FK_JobSkills_Jobs   FOREIGN KEY (JobId)   REFERENCES dbo.Jobs(JobId),
    CONSTRAINT FK_JobSkills_Skills FOREIGN KEY (SkillId) REFERENCES dbo.Skills(SkillId)
);
GO

-- 4) Extend existing Resumes table (ADD-only, all columns NULL / have DEFAULT)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Resumes') AND name = N'ResumeType')
BEGIN
    ALTER TABLE dbo.Resumes ADD
        ResumeType      NVARCHAR(10)  NOT NULL CONSTRAINT DF_Resumes_ResumeType DEFAULT N'File';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Resumes') AND name = N'DesiredTitle')
    ALTER TABLE dbo.Resumes ADD DesiredTitle NVARCHAR(150) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Resumes') AND name = N'CareerObjective')
    ALTER TABLE dbo.Resumes ADD CareerObjective NVARCHAR(MAX) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Resumes') AND name = N'Education')
    ALTER TABLE dbo.Resumes ADD Education NVARCHAR(MAX) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Resumes') AND name = N'WorkExperience')
    ALTER TABLE dbo.Resumes ADD WorkExperience NVARCHAR(MAX) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Resumes') AND name = N'Projects')
    ALTER TABLE dbo.Resumes ADD Projects NVARCHAR(MAX) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Resumes') AND name = N'Certifications')
    ALTER TABLE dbo.Resumes ADD Certifications NVARCHAR(MAX) NULL;
GO

-- Indexes to support job matching and CV classification
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ResumeSkills_SkillId' AND object_id = OBJECT_ID(N'dbo.ResumeSkills'))
    CREATE INDEX IX_ResumeSkills_SkillId ON dbo.ResumeSkills(SkillId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_JobSkills_SkillId' AND object_id = OBJECT_ID(N'dbo.JobSkills'))
    CREATE INDEX IX_JobSkills_SkillId ON dbo.JobSkills(SkillId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Resumes_ResumeType' AND object_id = OBJECT_ID(N'dbo.Resumes'))
    CREATE INDEX IX_Resumes_ResumeType ON dbo.Resumes(ResumeType);
GO