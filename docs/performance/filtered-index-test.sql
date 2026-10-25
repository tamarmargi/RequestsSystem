-- Experiment: can a filtered index for open requests be used by the API?
--
-- Candidate (created only for this experiment, on the database with just the primary key, and dropped after):
--   CREATE INDEX IX_Requests_Open_AssignedTo ON Requests (AssignedTo, CreatedAt DESC, Id DESC) WHERE Status <> 3;
--
-- The closest real API call is a handler's open work:
--   GET /api/requests?status=InProgress&assignedTo=דוד מזרחי
-- F1 is exactly what EF Core sends for it (same parameter naming and order as Q2/Q3 in queries.sql).
-- F2 is the same query with literals, which the API never sends, as a control.
--
-- Run: measure.ps1 -Label filtered-index -QueryFile filtered-index-test.sql -SkipApi

-- #query F1-parameterized | GET /api/requests?status=InProgress&assignedTo=דוד מזרחי (as EF sends it)
EXEC sp_executesql N'SELECT [r].[Id], [r].[AssignedTo], [r].[CreatedAt], [r].[OrganizationName], [r].[Priority], [r].[RowVersion], [r].[Status], [r].[Title], [r].[UpdatedAt]
FROM [Requests] AS [r]
WHERE [r].[Status] = @__filter_Status_0 AND [r].[AssignedTo] = @__filter_AssignedTo_1
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
OFFSET @__p_2 ROWS FETCH NEXT @__p_3 ROWS ONLY', N'@__filter_Status_0 tinyint,@__filter_AssignedTo_1 nvarchar(100),@__p_2 int,@__p_3 int', @__filter_Status_0 = 1, @__filter_AssignedTo_1 = N'דוד מזרחי', @__p_2 = 0, @__p_3 = 20
GO

-- #query F2-literal | the same query with literal values (control; not sent by the API)
SELECT [r].[Id], [r].[AssignedTo], [r].[CreatedAt], [r].[OrganizationName], [r].[Priority], [r].[RowVersion], [r].[Status], [r].[Title], [r].[UpdatedAt]
FROM [Requests] AS [r]
WHERE [r].[Status] = CAST(1 AS tinyint) AND [r].[AssignedTo] = N'דוד מזרחי'
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY
GO
