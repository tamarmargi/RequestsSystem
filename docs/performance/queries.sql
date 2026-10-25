-- Queries measured in the performance report (docs/performance/README.md).
--
-- Captured from the SQL Server plan cache (sys.dm_exec_cached_plans + sys.dm_exec_sql_text) after calling
-- each endpoint once through the API: the statement text and the parameter declarations are exactly what
-- EF Core 8 sends. EF sends a command without parameters as a plain batch (Q1-count, Q5-stats) and every
-- command with parameters through sp_executesql. Parameterization matters: the plan is compiled for
-- "Status = @p", not for a literal value.
--
-- Each block starts with "-- #query <id> | <endpoint>" and ends with GO; measure.ps1 splits the file on
-- these lines. Optional "-- #setup" / "-- #measure" / "-- #teardown" sections: only the #measure part is
-- measured (W1 uses this to run the update inside a rolled-back transaction).
--
-- Values: Status New = 0, High = 2 (tinyint); handler and organization are among the largest in the seed.

-- #query Q1-count | GET /api/requests?page=1&pageSize=20 (totalCount, no filter)
SELECT COUNT(*)
FROM [Requests] AS [r]
GO

-- #query Q1-page | GET /api/requests?page=1&pageSize=20 (page, default sort createdAt desc)
EXEC sp_executesql N'SELECT [r].[Id], [r].[AssignedTo], [r].[CreatedAt], [r].[OrganizationName], [r].[Priority], [r].[RowVersion], [r].[Status], [r].[Title], [r].[UpdatedAt]
FROM [Requests] AS [r]
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
OFFSET @__p_0 ROWS FETCH NEXT @__p_1 ROWS ONLY', N'@__p_0 int,@__p_1 int', @__p_0 = 0, @__p_1 = 20
GO

-- #query Q2-count | GET /api/requests?status=New&priority=High&page=1&pageSize=20 (totalCount)
EXEC sp_executesql N'SELECT COUNT(*)
FROM [Requests] AS [r]
WHERE [r].[Status] = @__filter_Status_0 AND [r].[Priority] = @__filter_Priority_1', N'@__filter_Status_0 tinyint,@__filter_Priority_1 tinyint', @__filter_Status_0 = 0, @__filter_Priority_1 = 2
GO

-- #query Q2-page | GET /api/requests?status=New&priority=High&page=1&pageSize=20 (page)
EXEC sp_executesql N'SELECT [r].[Id], [r].[AssignedTo], [r].[CreatedAt], [r].[OrganizationName], [r].[Priority], [r].[RowVersion], [r].[Status], [r].[Title], [r].[UpdatedAt]
FROM [Requests] AS [r]
WHERE [r].[Status] = @__filter_Status_0 AND [r].[Priority] = @__filter_Priority_1
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
OFFSET @__p_2 ROWS FETCH NEXT @__p_3 ROWS ONLY', N'@__filter_Status_0 tinyint,@__filter_Priority_1 tinyint,@__p_2 int,@__p_3 int', @__filter_Status_0 = 0, @__filter_Priority_1 = 2, @__p_2 = 0, @__p_3 = 20
GO

-- #query Q3-count | GET /api/requests?assignedTo=דוד מזרחי&page=1&pageSize=20 (totalCount)
EXEC sp_executesql N'SELECT COUNT(*)
FROM [Requests] AS [r]
WHERE [r].[AssignedTo] = @__filter_AssignedTo_0', N'@__filter_AssignedTo_0 nvarchar(100)', @__filter_AssignedTo_0 = N'דוד מזרחי'
GO

-- #query Q3-page | GET /api/requests?assignedTo=דוד מזרחי&page=1&pageSize=20 (page)
EXEC sp_executesql N'SELECT [r].[Id], [r].[AssignedTo], [r].[CreatedAt], [r].[OrganizationName], [r].[Priority], [r].[RowVersion], [r].[Status], [r].[Title], [r].[UpdatedAt]
FROM [Requests] AS [r]
WHERE [r].[AssignedTo] = @__filter_AssignedTo_0
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
OFFSET @__p_1 ROWS FETCH NEXT @__p_2 ROWS ONLY', N'@__filter_AssignedTo_0 nvarchar(100),@__p_1 int,@__p_2 int', @__filter_AssignedTo_0 = N'דוד מזרחי', @__p_1 = 0, @__p_2 = 20
GO

-- #query Q4-count | GET /api/requests?organizationName=Jordan Valley Logistics Ltd&page=1&pageSize=20 (totalCount)
EXEC sp_executesql N'SELECT COUNT(*)
FROM [Requests] AS [r]
WHERE [r].[OrganizationName] = @__filter_OrganizationName_0', N'@__filter_OrganizationName_0 nvarchar(200)', @__filter_OrganizationName_0 = N'Jordan Valley Logistics Ltd'
GO

-- #query Q4-page | GET /api/requests?organizationName=Jordan Valley Logistics Ltd&page=1&pageSize=20 (page)
EXEC sp_executesql N'SELECT [r].[Id], [r].[AssignedTo], [r].[CreatedAt], [r].[OrganizationName], [r].[Priority], [r].[RowVersion], [r].[Status], [r].[Title], [r].[UpdatedAt]
FROM [Requests] AS [r]
WHERE [r].[OrganizationName] = @__filter_OrganizationName_0
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
OFFSET @__p_1 ROWS FETCH NEXT @__p_2 ROWS ONLY', N'@__filter_OrganizationName_0 nvarchar(200),@__p_1 int,@__p_2 int', @__filter_OrganizationName_0 = N'Jordan Valley Logistics Ltd', @__p_1 = 0, @__p_2 = 20
GO

-- #query Q5-stats | GET /api/requests/stats (first, uncached call)
SELECT COUNT(*), COUNT(CASE
    WHEN [t].[Status] = CAST(0 AS tinyint) THEN 1
END), COUNT(CASE
    WHEN [t].[Status] = CAST(1 AS tinyint) THEN 1
END), COUNT(CASE
    WHEN [t].[Status] = CAST(2 AS tinyint) THEN 1
END), COUNT(CASE
    WHEN [t].[Status] = CAST(3 AS tinyint) THEN 1
END), COUNT(CASE
    WHEN [t].[Priority] = CAST(0 AS tinyint) THEN 1
END), COUNT(CASE
    WHEN [t].[Priority] = CAST(1 AS tinyint) THEN 1
END), COUNT(CASE
    WHEN [t].[Priority] = CAST(2 AS tinyint) THEN 1
END)
FROM (
    SELECT [r].[Priority], [r].[Status], 1 AS [Key]
    FROM [Requests] AS [r]
) AS [t]
GROUP BY [t].[Key]
GO

-- #query Q6-count | GET /api/requests?search=תלונה&page=1&pageSize=20 (totalCount)
EXEC sp_executesql N'SELECT COUNT(*)
FROM [Requests] AS [r]
WHERE [r].[Title] LIKE @__filter_Search_0_contains ESCAPE N''\'' OR [r].[OrganizationName] LIKE @__filter_Search_0_contains ESCAPE N''\''', N'@__filter_Search_0_contains nvarchar(200)', @__filter_Search_0_contains = N'%תלונה%'
GO

-- #query Q6-page | GET /api/requests?search=תלונה&page=1&pageSize=20 (page)
EXEC sp_executesql N'SELECT [r].[Id], [r].[AssignedTo], [r].[CreatedAt], [r].[OrganizationName], [r].[Priority], [r].[RowVersion], [r].[Status], [r].[Title], [r].[UpdatedAt]
FROM [Requests] AS [r]
WHERE [r].[Title] LIKE @__filter_Search_0_contains ESCAPE N''\'' OR [r].[OrganizationName] LIKE @__filter_Search_0_contains ESCAPE N''\''
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
OFFSET @__p_1 ROWS FETCH NEXT @__p_2 ROWS ONLY', N'@__filter_Search_0_contains nvarchar(200),@__p_1 int,@__p_2 int', @__filter_Search_0_contains = N'%תלונה%', @__p_1 = 0, @__p_2 = 20
GO

-- #query Q7-page | GET /api/requests?page=4000&pageSize=20 (deep paging; its COUNT is identical to Q1-count)
EXEC sp_executesql N'SELECT [r].[Id], [r].[AssignedTo], [r].[CreatedAt], [r].[OrganizationName], [r].[Priority], [r].[RowVersion], [r].[Status], [r].[Title], [r].[UpdatedAt]
FROM [Requests] AS [r]
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
OFFSET @__p_0 ROWS FETCH NEXT @__p_1 ROWS ONLY', N'@__p_0 int,@__p_1 int', @__p_0 = 79980, @__p_1 = 20
GO

-- #query W1-update | PATCH /api/requests/4088/status InProgress -> Waiting (UPDATE + history INSERT; rolled back, data unchanged)
-- #setup
BEGIN TRANSACTION;
DECLARE @version varbinary(8) = (SELECT [RowVersion] FROM [Requests] WHERE [Id] = 4088);
DECLARE @now datetime2(7) = SYSUTCDATETIME();
-- #measure
EXEC sp_executesql N'SET NOCOUNT ON;
INSERT INTO [RequestStatusHistory] ([ChangedAt], [ChangedBy], [NewStatus], [PreviousStatus], [RequestId])
OUTPUT INSERTED.[Id]
VALUES (@p0, @p1, @p2, @p3, @p4);
UPDATE [Requests] SET [Status] = @p5, [UpdatedAt] = @p6
OUTPUT INSERTED.[RowVersion]
WHERE [Id] = @p7 AND [RowVersion] = @p8;
', N'@p0 datetime2(7),@p1 nvarchar(100),@p2 tinyint,@p3 tinyint,@p4 int,@p7 int,@p8 varbinary(8),@p5 tinyint,@p6 datetime2(7)', @p0 = @now, @p1 = N'perf', @p2 = 2, @p3 = 1, @p4 = 4088, @p7 = 4088, @p8 = @version, @p5 = 2, @p6 = @now
-- #teardown
ROLLBACK TRANSACTION;
GO
