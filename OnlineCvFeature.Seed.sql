/* ============================================================================
   FEATURE: ONLINE-CV - Seed sample skills (~20 items)
   ----------------------------------------------------------------------------
   Run ONLY when the Skills table is empty. Safe to re-run (WHERE NOT EXISTS
   guard per name). Skills are shared by online CVs and job posts.
   ============================================================================ */

IF ((SELECT COUNT(*) FROM dbo.Skills) = 0)
BEGIN
    INSERT INTO dbo.Skills (SkillName, IsActive) VALUES
    (N'C#',          1),
    (N'.NET',        1),
    (N'SQL Server',  1),
    (N'JavaScript',  1),
    (N'Java',        1),
    (N'PHP',         1),
    (N'HTML/CSS',    1),
    (N'Python',      1),
    (N'Excel',       1),
    (N'Word',        1),
    (N'PowerPoint',  1),
    (N'Tiếng Anh',   1),
    (N'Giao tiếp',   1),
    (N'Phục vụ',     1),
    (N'Pha chế',     1),
    (N'Bán hàng',    1),
    (N'Lái xe',      1),
    (N'Giao hàng',   1),
    (N'Gia sư',      1),
    (N'IT Support',  1),
    (N'Canva/TK',    1);
END
GO