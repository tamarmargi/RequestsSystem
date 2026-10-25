-- Search by request number: cost of the extra "OR [r].[Id] = @id" in the search query.
--
-- Captured from the plan cache like queries.sql. A text search (תלונה) sends exactly the same SQL as before
-- the change (= Q6 in queries.sql). A numeric search (48213) sent the plain LIKE query before the change
-- ("before" blocks) and now sends the LIKE query with "OR [r].[Id] = @__id_1" ("after" blocks).
-- Run: measure.ps1 -Label search-number -QueryFile search-number-tests.sql -SkipApi

-- #query S1-text-count | ?search=תלונה (totalCount; unchanged by this feature)
EXEC sp_executesql N'SELECT COUNT(*)
FROM [Requests] AS [r]
WHERE [r].[Title] LIKE @__filter_Search_0_contains ESCAPE N''\'' OR [r].[OrganizationName] LIKE @__filter_Search_0_contains ESCAPE N''\''', N'@__filter_Search_0_contains nvarchar(200)', @__filter_Search_0_contains = N'%תלונה%'
GO

-- #query S1-text-page | ?search=תלונה (page; unchanged by this feature)
EXEC sp_executesql N'SELECT [r].[Id], [r].[AssignedTo], [r].[CreatedAt], [r].[OrganizationName], [r].[Priority], [r].[RowVersion], [r].[Status], [r].[Title], [r].[UpdatedAt]
FROM [Requests] AS [r]
WHERE [r].[Title] LIKE @__filter_Search_0_contains ESCAPE N''\'' OR [r].[OrganizationName] LIKE @__filter_Search_0_contains ESCAPE N''\''
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
OFFSET @__p_1 ROWS FETCH NEXT @__p_2 ROWS ONLY', N'@__filter_Search_0_contains nvarchar(200),@__p_1 int,@__p_2 int', @__filter_Search_0_contains = N'%תלונה%', @__p_1 = 0, @__p_2 = 20
GO

-- #query S2-number-before-count | ?search=48213 before the change (text match only; totalCount 0)
EXEC sp_executesql N'SELECT COUNT(*)
FROM [Requests] AS [r]
WHERE [r].[Title] LIKE @__filter_Search_0_contains ESCAPE N''\'' OR [r].[OrganizationName] LIKE @__filter_Search_0_contains ESCAPE N''\''', N'@__filter_Search_0_contains nvarchar(200)', @__filter_Search_0_contains = N'%48213%'
GO

-- #query S2-number-before-page | ?search=48213 before the change (page)
EXEC sp_executesql N'SELECT [r].[Id], [r].[AssignedTo], [r].[CreatedAt], [r].[OrganizationName], [r].[Priority], [r].[RowVersion], [r].[Status], [r].[Title], [r].[UpdatedAt]
FROM [Requests] AS [r]
WHERE [r].[Title] LIKE @__filter_Search_0_contains ESCAPE N''\'' OR [r].[OrganizationName] LIKE @__filter_Search_0_contains ESCAPE N''\''
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
OFFSET @__p_1 ROWS FETCH NEXT @__p_2 ROWS ONLY', N'@__filter_Search_0_contains nvarchar(200),@__p_1 int,@__p_2 int', @__filter_Search_0_contains = N'%48213%', @__p_1 = 0, @__p_2 = 20
GO

-- #query S3-number-after-count | ?search=48213 after the change (text OR Id; totalCount 1)
EXEC sp_executesql N'SELECT COUNT(*)
FROM [Requests] AS [r]
WHERE [r].[Title] LIKE @__search_0_contains ESCAPE N''\'' OR [r].[OrganizationName] LIKE @__search_0_contains ESCAPE N''\'' OR [r].[Id] = @__id_1', N'@__search_0_contains nvarchar(200),@__id_1 int', @__search_0_contains = N'%48213%', @__id_1 = 48213
GO

-- #query S3-number-after-page | ?search=48213 after the change (page)
EXEC sp_executesql N'SELECT [r].[Id], [r].[AssignedTo], [r].[CreatedAt], [r].[OrganizationName], [r].[Priority], [r].[RowVersion], [r].[Status], [r].[Title], [r].[UpdatedAt]
FROM [Requests] AS [r]
WHERE [r].[Title] LIKE @__search_0_contains ESCAPE N''\'' OR [r].[OrganizationName] LIKE @__search_0_contains ESCAPE N''\'' OR [r].[Id] = @__id_1
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
OFFSET @__p_2 ROWS FETCH NEXT @__p_3 ROWS ONLY', N'@__search_0_contains nvarchar(200),@__id_1 int,@__p_2 int,@__p_3 int', @__search_0_contains = N'%48213%', @__id_1 = 48213, @__p_2 = 0, @__p_3 = 20
GO
