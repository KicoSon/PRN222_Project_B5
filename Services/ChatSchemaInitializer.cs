using Microsoft.EntityFrameworkCore;
using StudentPartTime.Models;

namespace StudentPartTime.Services;

public static class ChatSchemaInitializer
{
    public static async Task EnsureCreatedAsync(StudentPartTimeJobDbContext context, ILogger logger)
    {
        try
        {
            var sql = @"
IF OBJECT_ID(N'dbo.ChatRooms', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatRooms
    (
        ChatRoomId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChatRooms PRIMARY KEY,
        ApplicationId INT NOT NULL,
        JobId INT NOT NULL,
        StudentId INT NOT NULL,
        EmployerId INT NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ChatRooms_CreatedAt DEFAULT SYSDATETIME(),
        LastMessageAt DATETIME2(0) NULL,
        CONSTRAINT UQ_ChatRooms_ApplicationId UNIQUE (ApplicationId),
        CONSTRAINT FK_ChatRooms_Applications FOREIGN KEY (ApplicationId) REFERENCES dbo.Applications(ApplicationId) ON DELETE CASCADE,
        CONSTRAINT FK_ChatRooms_Jobs FOREIGN KEY (JobId) REFERENCES dbo.Jobs(JobId),
        CONSTRAINT FK_ChatRooms_Students FOREIGN KEY (StudentId) REFERENCES dbo.Students(StudentId),
        CONSTRAINT FK_ChatRooms_Employers FOREIGN KEY (EmployerId) REFERENCES dbo.Employers(EmployerId)
    );
END;

IF OBJECT_ID(N'dbo.ChatMessages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatMessages
    (
        ChatMessageId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChatMessages PRIMARY KEY,
        ChatRoomId INT NOT NULL,
        SenderUserId INT NULL,
        Content NVARCHAR(1000) NOT NULL,
        IsSystemMessage BIT NOT NULL CONSTRAINT DF_ChatMessages_IsSystemMessage DEFAULT 0,
        IsFlagged BIT NOT NULL CONSTRAINT DF_ChatMessages_IsFlagged DEFAULT 0,
        IsReadByStudent BIT NOT NULL CONSTRAINT DF_ChatMessages_IsReadByStudent DEFAULT 0,
        IsReadByEmployer BIT NOT NULL CONSTRAINT DF_ChatMessages_IsReadByEmployer DEFAULT 0,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ChatMessages_CreatedAt DEFAULT SYSDATETIME(),
        CONSTRAINT FK_ChatMessages_ChatRooms FOREIGN KEY (ChatRoomId) REFERENCES dbo.ChatRooms(ChatRoomId) ON DELETE CASCADE,
        CONSTRAINT FK_ChatMessages_Users FOREIGN KEY (SenderUserId) REFERENCES dbo.Users(UserId)
    );
    CREATE INDEX IX_ChatMessages_ChatRoomId ON dbo.ChatMessages(ChatRoomId);
END;

IF OBJECT_ID(N'dbo.ChatRooms', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ChatRooms_StudentId' AND object_id = OBJECT_ID(N'dbo.ChatRooms'))
    CREATE INDEX IX_ChatRooms_StudentId ON dbo.ChatRooms(StudentId);

IF OBJECT_ID(N'dbo.ChatRooms', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ChatRooms_EmployerId' AND object_id = OBJECT_ID(N'dbo.ChatRooms'))
    CREATE INDEX IX_ChatRooms_EmployerId ON dbo.ChatRooms(EmployerId);

IF OBJECT_ID(N'dbo.ChatRooms', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ChatRooms_LastMessageAt' AND object_id = OBJECT_ID(N'dbo.ChatRooms'))
    CREATE INDEX IX_ChatRooms_LastMessageAt ON dbo.ChatRooms(LastMessageAt);
";
            await context.Database.ExecuteSqlRawAsync(sql);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chat schema initialization failed. Run Database/ChatSchema.sql manually if needed.");
        }
    }
}
