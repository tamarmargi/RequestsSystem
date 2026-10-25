<#
.SYNOPSIS
    Measures the queries in queries.sql against the local SQL Server (Docker) and writes
    results-<Label>.md plus the actual execution plans to plans/<Label>/<query>.sqlplan.

.DESCRIPTION
    For every query block in queries.sql:
      - runs it <Runs> times with SET STATISTICS IO/TIME ON (median CPU and elapsed time; logical reads per
        table from the first run, they do not change between runs);
      - runs it once more with SET STATISTICS XML ON and saves the actual plan;
      - lists the main plan operators and the indexes they use.
    If an API is reachable at -ApiBaseUrl, it also measures end-to-end response times (median of <ApiCalls>
    calls) for Q1, Q2, Q5 and Q7. Start that API with the stats cache disabled, so every Q5 call hits the
    database:  $env:Cache__StatsSeconds = '0'; dotnet run --project backend/src/Requests.Api

    sqlcmd runs inside the container (docker compose exec); the sa password is read from the container's
    MSSQL_SA_PASSWORD variable, so it is not stored in this script.

.EXAMPLE
    ./docs/performance/measure.ps1 -Label before

.EXAMPLE
    ./docs/performance/measure.ps1 -Label filtered-index -QueryFile filtered-index-test.sql -SkipApi
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('before', 'after', 'filtered-index', 'bottlenecks')]
    [string] $Label,

    [string] $ApiBaseUrl = 'http://localhost:5000',

    [int] $Runs = 5,

    [int] $ApiCalls = 20,

    # Relative to this folder.
    [string] $QueryFile = 'queries.sql',

    [switch] $SkipApi
)

$here = $PSScriptRoot
$composeFile = Join-Path (Resolve-Path (Join-Path $here '..\..')) 'docker-compose.yml'
$utf8 = New-Object System.Text.UTF8Encoding $false
$work = Join-Path ([IO.Path]::GetTempPath()) "requests-perf-$Label"
$planDir = Join-Path $here "plans\$Label"
New-Item -ItemType Directory -Force $work, $planDir | Out-Null

# End-to-end API timings: endpoint per query group (both COUNT and page query run in one call).
$apiUrls = [ordered]@{
    'Q1' = '/api/requests?page=1&pageSize=20'
    'Q2' = '/api/requests?status=New&priority=High&page=1&pageSize=20'
    'Q5' = '/api/requests/stats'
    'Q7' = '/api/requests?page=4000&pageSize=20'
}

$showplanNs = 'http://schemas.microsoft.com/sqlserver/2004/07/showplan'

function Invoke-Sql {
    param([string] $Name, [string] $Sql, [string] $Options = '-y 0')

    $local = Join-Path $work "$Name.sql"
    [IO.File]::WriteAllText($local, $Sql, $utf8)
    # docker compose cp reports progress on stderr; only the exit code matters.
    docker compose -f $composeFile cp $local "sqlserver:/tmp/$Name.sql" 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "docker compose cp failed for $Name" }

    # -I: QUOTED_IDENTIFIER ON, as in SqlClient/EF sessions (sqlcmd defaults to OFF, which also makes SQL Server
    #     ignore filtered indexes); -x: no sqlcmd variable substitution; -f 65001: UTF-8; -b: fail on SQL errors.
    # No double quotes in the command: Windows PowerShell does not escape them for native commands. A bash
    # assignment is not word-split, so SQLCMDPASSWORD=$MSSQL_SA_PASSWORD is safe without quotes.
    $command = 'SQLCMDPASSWORD=$MSSQL_SA_PASSWORD /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -d RequestsDb ' +
        "-I -x -b -f 65001 $Options -i /tmp/$Name.sql -o /tmp/$Name.out"
    docker compose -f $composeFile exec -T sqlserver bash -c $command | Out-Null
    $exitCode = $LASTEXITCODE

    $out = Join-Path $work "$Name.out"
    docker compose -f $composeFile cp "sqlserver:/tmp/$Name.out" $out 2>$null | Out-Null
    $text = [IO.File]::ReadAllText($out, $utf8)
    if ($exitCode -ne 0) { throw "sqlcmd failed for ${Name}:`n$text" }
    return $text
}

function Get-QueryBlocks {
    $text = [IO.File]::ReadAllText((Join-Path $here $QueryFile), $utf8)
    $pattern = '(?ms)^-- #query (?<id>\S+) \| (?<endpoint>[^\r\n]+)\r?\n(?<body>.*?)^GO\s*$'
    foreach ($m in [regex]::Matches($text, $pattern)) {
        $body = $m.Groups['body'].Value
        $setup = ''; $teardown = ''; $measure = $body
        if ($body -match '(?ms)^-- #setup\r?\n(?<s>.*?)^-- #measure\r?\n(?<m>.*?)^-- #teardown\r?\n(?<t>.*)$') {
            $setup = $Matches['s']; $measure = $Matches['m']; $teardown = $Matches['t']
        }
        [pscustomobject]@{
            Id       = $m.Groups['id'].Value
            Endpoint = $m.Groups['endpoint'].Value.Trim()
            Setup    = $setup
            Measure  = $measure
            Teardown = $teardown
        }
    }
}

function Get-Median([double[]] $values) {
    $sorted = $values | Sort-Object
    $n = $sorted.Count
    if ($n -eq 0) { return $null }
    if ($n % 2 -eq 1) { return $sorted[[int][math]::Floor($n / 2)] }
    return ($sorted[$n / 2 - 1] + $sorted[$n / 2]) / 2
}

function Measure-Statistics($query) {
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine('SET NOCOUNT ON;').AppendLine('GO')
    for ($i = 1; $i -le $Runs; $i++) {
        [void]$sb.AppendLine("PRINT '#RUN $i';").AppendLine('GO')
        [void]$sb.AppendLine($query.Setup)
        [void]$sb.AppendLine('SET STATISTICS IO ON;').AppendLine('SET STATISTICS TIME ON;')
        [void]$sb.AppendLine($query.Measure)
        [void]$sb.AppendLine('SET STATISTICS TIME OFF;').AppendLine('SET STATISTICS IO OFF;')
        [void]$sb.AppendLine($query.Teardown).AppendLine('GO')
    }
    [void]$sb.AppendLine("PRINT '#END';").AppendLine('GO')

    $output = Invoke-Sql -Name "$($query.Id)-stats" -Sql $sb.ToString()

    $run = 0; $mode = ''
    $times = @{}      # run -> list of @(cpu, elapsed) for each "Execution Times" entry
    $reads = [ordered]@{}  # table -> logical reads (first run)
    foreach ($line in $output -split "\r?\n") {
        if ($line -match '^#RUN (\d+)') { $run = [int]$Matches[1]; $times[$run] = @(); continue }
        if ($line -match '^#END') { break }
        if ($run -eq 0) { continue }
        if ($line -match 'SQL Server Execution Times') { $mode = 'exec'; continue }
        if ($line -match 'SQL Server parse and compile time') { $mode = 'compile'; continue }
        if ($line -match 'CPU time = (\d+) ms,\s+elapsed time = (\d+) ms') {
            if ($mode -eq 'exec') { $times[$run] += , @([int]$Matches[1], [int]$Matches[2]) }
            $mode = ''
            continue
        }
        if ($run -eq 1 -and $line -match "^Table '([^']+)'\. Scan count \d+, logical reads (\d+)") {
            $table = $Matches[1]
            $reads[$table] = [int]$reads[$table] + [int]$Matches[2]
        }
    }

    # Per run, the entry with the largest elapsed time is the whole statement (sp_executesql reports the
    # inner statement and the outer call; the outer one includes the inner one).
    $cpu = @(); $elapsed = @()
    foreach ($r in $times.Keys) {
        $entries = $times[$r]
        if ($entries.Count -eq 0) { continue }
        $top = $entries | Sort-Object { $_[1] } -Descending | Select-Object -First 1
        $cpu += $top[0]; $elapsed += $top[1]
    }

    [pscustomobject]@{ Reads = $reads; CpuMs = (Get-Median $cpu); ElapsedMs = (Get-Median $elapsed) }
}

function Save-Plan($query) {
    $sql = "SET NOCOUNT ON;`nGO`n$($query.Setup)`nSET STATISTICS XML ON;`n$($query.Measure)`nSET STATISTICS XML OFF;`n$($query.Teardown)`nGO`n"
    $output = Invoke-Sql -Name "$($query.Id)-plan" -Sql $sql
    $plans = [regex]::Matches($output, '(?s)<ShowPlanXML.*?</ShowPlanXML>')
    if ($plans.Count -eq 0) { throw "No execution plan returned for $($query.Id)" }

    # One file per query: statements of a multi-statement batch (W1) are merged into the first plan.
    $doc = New-Object System.Xml.XmlDocument
    $doc.LoadXml($plans[0].Value)
    $ns = New-Object System.Xml.XmlNamespaceManager $doc.NameTable
    $ns.AddNamespace('sp', $showplanNs)
    $statements = $doc.SelectSingleNode('//sp:BatchSequence/sp:Batch/sp:Statements', $ns)
    for ($i = 1; $i -lt $plans.Count; $i++) {
        $other = New-Object System.Xml.XmlDocument
        $other.LoadXml($plans[$i].Value)
        foreach ($stmt in $other.SelectNodes('//sp:BatchSequence/sp:Batch/sp:Statements/*', $ns)) {
            [void]$statements.AppendChild($doc.ImportNode($stmt, $true))
        }
    }

    $path = Join-Path $planDir "$($query.Id).sqlplan"
    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Encoding = $utf8
    $settings.Indent = $true
    $writer = [System.Xml.XmlWriter]::Create($path, $settings)
    $doc.Save($writer)
    $writer.Close()

    # Main operators in plan order (Compute Scalar is noise here), with the indexes they touch.
    $operators = New-Object System.Collections.Generic.List[string]
    $indexes = New-Object System.Collections.Generic.List[string]
    foreach ($relOp in $doc.SelectNodes('//sp:RelOp', $ns)) {
        $op = $relOp.GetAttribute('PhysicalOp')
        if ($op -eq 'Compute Scalar') { continue }
        $objects = @($relOp.SelectNodes('./*/sp:Object', $ns) | ForEach-Object { $_.GetAttribute('Index').Trim('[', ']') } | Where-Object { $_ })
        $text = if ($objects.Count -gt 0) { "$op ($($objects -join ', '))" } else { $op }
        if (-not $operators.Contains($text)) { $operators.Add($text) }
        foreach ($index in $objects) { if (-not $indexes.Contains($index)) { $indexes.Add($index) } }
    }

    [pscustomobject]@{ Operators = ($operators -join ' > '); Indexes = ($indexes -join ', ') }
}

function Measure-Api {
    Add-Type -AssemblyName System.Net.Http
    $client = New-Object System.Net.Http.HttpClient
    $client.Timeout = [TimeSpan]::FromSeconds(30)
    try {
        $health = $client.GetAsync("$ApiBaseUrl/api/health").GetAwaiter().GetResult()
        if (-not $health.IsSuccessStatusCode) { throw 'not healthy' }
    }
    catch {
        Write-Warning "API not reachable at $ApiBaseUrl - API timings are skipped."
        return @{}
    }

    $result = @{}
    foreach ($key in $apiUrls.Keys) {
        $url = "$ApiBaseUrl$($apiUrls[$key])"
        for ($i = 0; $i -lt 2; $i++) { [void]$client.GetStringAsync($url).GetAwaiter().GetResult() } # warm-up
        $samples = @()
        for ($i = 0; $i -lt $ApiCalls; $i++) {
            $sw = [Diagnostics.Stopwatch]::StartNew()
            $response = $client.GetAsync($url).GetAwaiter().GetResult()
            [void]$response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            $sw.Stop()
            if (-not $response.IsSuccessStatusCode) { throw "$url returned $([int]$response.StatusCode)" }
            $samples += $sw.Elapsed.TotalMilliseconds
        }
        $result[$key] = [math]::Round((Get-Median $samples), 1)
    }

    if ($result['Q5'] -lt 5) {
        Write-Warning "Q5 median is $($result['Q5']) ms: the stats look cached. Start the API with Cache__StatsSeconds=0."
    }
    return $result
}

# --- Run ------------------------------------------------------------------------------------------------

Write-Host "Measuring '$Label' ($Runs runs per query)..."

$indexInfo = Invoke-Sql -Name 'indexes' -Options '-h -1 -W -s ^' -Sql @'
SET NOCOUNT ON;
SELECT i.name,
       STRING_AGG(c.name + CASE WHEN ic.is_descending_key = 1 THEN ' DESC' ELSE '' END, ', ')
           WITHIN GROUP (ORDER BY ic.key_ordinal),
       ISNULL(i.filter_definition, '')
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.key_ordinal > 0
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID('Requests')
GROUP BY i.index_id, i.name, i.filter_definition
ORDER BY i.index_id;
SELECT 'counts', (SELECT COUNT(*) FROM Requests), (SELECT COUNT(*) FROM RequestStatusHistory);
'@

$indexRows = @(); $requestCount = '?'; $historyCount = '?'
foreach ($line in ($indexInfo -split "\r?\n" | Where-Object { $_.Trim() })) {
    $parts = $line.Split('^')
    if ($parts[0] -eq 'counts') { $requestCount = $parts[1]; $historyCount = $parts[2]; continue }
    $filter = if ($parts.Count -gt 2 -and $parts[2]) { " WHERE $($parts[2])" } else { '' }
    $indexRows += "- ``$($parts[0])``: ($($parts[1]))$filter"
}

$api = if ($SkipApi) { @{} } else { Measure-Api }

$rows = @()
foreach ($query in Get-QueryBlocks) {
    Write-Host "  $($query.Id)"
    $stats = Measure-Statistics $query
    $plan = Save-Plan $query

    $requestsReads = if ($stats.Reads.Contains('Requests')) { $stats.Reads['Requests'] } else { 0 }
    $other = ($stats.Reads.Keys | Where-Object { $_ -ne 'Requests' } | ForEach-Object { "$_ $($stats.Reads[$_])" }) -join ', '
    $group = $query.Id.Split('-')[0]
    $apiMs = if (($query.Id -like '*-page' -or $query.Id -eq 'Q5-stats') -and $api.ContainsKey($group)) { $api[$group] } else { '' }

    $rows += [pscustomobject]@{
        Id = $query.Id; Endpoint = $query.Endpoint; Reads = $requestsReads; Other = $other
        Cpu = $stats.CpuMs; Elapsed = $stats.ElapsedMs; Api = $apiMs
        Operators = $plan.Operators; Indexes = $plan.Indexes
    }
}

$md = New-Object System.Text.StringBuilder
[void]$md.AppendLine("# Performance results: $Label").AppendLine()
[void]$md.AppendLine("Generated by ``docs/performance/measure.ps1 -Label $Label`` on $(Get-Date -Format 'yyyy-MM-dd HH:mm').").AppendLine()
[void]$md.AppendLine("- Data: $requestCount requests, $historyCount history rows.")
[void]$md.AppendLine("- SQL: median of $Runs runs (warm buffer pool); logical reads from one run. Plans: ``plans/$Label/<query>.sqlplan``.")
[void]$md.AppendLine("- API: median of $ApiCalls calls per endpoint after 2 warm-up calls, whole endpoint (COUNT + page query), stats cache disabled; shown on the page / stats row.").AppendLine()
[void]$md.AppendLine('Indexes on `Requests`:').AppendLine()
foreach ($row in $indexRows) { [void]$md.AppendLine($row) }
[void]$md.AppendLine()
[void]$md.AppendLine('| Query | Endpoint | Logical reads (Requests) | Other reads | CPU ms | Elapsed ms | API median ms | Main plan operators | Index used |')
[void]$md.AppendLine('|---|---|---:|---|---:|---:|---:|---|---|')
foreach ($r in $rows) {
    [void]$md.AppendLine("| $($r.Id) | $($r.Endpoint) | $($r.Reads) | $($r.Other) | $($r.Cpu) | $($r.Elapsed) | $($r.Api) | $($r.Operators) | $($r.Indexes) |")
}

$resultPath = Join-Path $here "results-$Label.md"
[IO.File]::WriteAllText($resultPath, $md.ToString(), $utf8)
Write-Host "Wrote $resultPath and $($rows.Count) plans to $planDir"
