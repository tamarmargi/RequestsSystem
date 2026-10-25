-- Controls for the deep-paging bottleneck (Q7 = GET /api/requests?page=4000&pageSize=20). These are NOT what the
-- API sends; they measure the alternatives proposed in the report. Run with all indexes in place:
--   measure.ps1 -Label bottlenecks -QueryFile bottleneck-tests.sql -SkipApi

-- #query B1-offset-recompile | Q7 compiled for its own offset (OPTION (RECOMPILE)), no plan reuse from page 1
EXEC sp_executesql N'SELECT [r].[Id], [r].[AssignedTo], [r].[CreatedAt], [r].[OrganizationName], [r].[Priority], [r].[RowVersion], [r].[Status], [r].[Title], [r].[UpdatedAt]
FROM [Requests] AS [r]
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
OFFSET @__p_0 ROWS FETCH NEXT @__p_1 ROWS ONLY
OPTION (RECOMPILE)', N'@__p_0 int,@__p_1 int', @__p_0 = 79980, @__p_1 = 20
GO

-- #query B2-keyset | the same page with keyset (seek) pagination: continue after the last row of page 3999
-- #setup
DECLARE @lastCreatedAt datetime2(7), @lastId int;
SELECT @lastCreatedAt = [CreatedAt], @lastId = [Id]
FROM [Requests]
ORDER BY [CreatedAt] DESC, [Id] DESC
OFFSET 79979 ROWS FETCH NEXT 1 ROWS ONLY;
-- #measure
EXEC sp_executesql N'SELECT TOP(@__p_2) [r].[Id], [r].[AssignedTo], [r].[CreatedAt], [r].[OrganizationName], [r].[Priority], [r].[RowVersion], [r].[Status], [r].[Title], [r].[UpdatedAt]
FROM [Requests] AS [r]
WHERE [r].[CreatedAt] < @__lastCreatedAt_0 OR ([r].[CreatedAt] = @__lastCreatedAt_0 AND [r].[Id] < @__lastId_1)
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC', N'@__lastCreatedAt_0 datetime2(7),@__lastId_1 int,@__p_2 int', @__lastCreatedAt_0 = @lastCreatedAt, @__lastId_1 = @lastId, @__p_2 = 20
-- #teardown
GO
