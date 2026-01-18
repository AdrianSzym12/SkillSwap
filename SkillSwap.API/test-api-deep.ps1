param(
  [string]$BaseUrl = "https://localhost:7291"
)


# =========================
# Helpers (PowerShell 5.1 compatible)
# =========================
$ErrorActionPreference = "Stop"

function Write-Section([string]$t) {
  Write-Host ""
  Write-Host ("=== {0} ===" -f $t)
}
function Write-OK([string]$t)   { Write-Host ("[OK]  {0}" -f $t) }
function Write-Warn([string]$t) { Write-Host ("[WARN] {0}" -f $t) }
function Write-Fail([string]$t) { Write-Host ("[FAIL] {0}" -f $t) }

function To-JsonBody($obj) {
  return ($obj | ConvertTo-Json -Depth 20)
}

function Invoke-Api {
  param(
    [ValidateSet("GET","POST","PUT","PATCH","DELETE")]
    [string]$Method,
    [string]$Url,
    [hashtable]$Headers = $null,
    $Body = $null,
    [int]$TimeoutSec = 30
  )
  $res = [ordered]@{
    Ok = $false
    Status = $null
    Content = $null
    Error = $null
    Headers = $null
  }

  try {
    $params = @{
      Method      = $Method
      Uri         = $Url
      TimeoutSec  = $TimeoutSec
      ErrorAction = "Stop"
    }
    if ($Headers) { $params["Headers"] = $Headers }
    if ($Body -ne $null) {
      $params["Body"] = $Body
      $params["ContentType"] = "application/json"
    }

    $r = Invoke-WebRequest @params
    $res.Status  = [int]$r.StatusCode
    $res.Content = $r.Content
    $res.Headers = $r.Headers
    $res.Ok = ($res.Status -ge 200 -and $res.Status -lt 300)
  }
  catch {
    $res.Error = $_.Exception.Message

    # Try to extract HTTP status + body if present
    try {
      if ($_.Exception.Response -ne $null) {
        $resp = $_.Exception.Response
        $res.Status = [int]$resp.StatusCode
        $stream = $resp.GetResponseStream()
        if ($stream) {
          $reader = New-Object System.IO.StreamReader($stream)
          $res.Content = $reader.ReadToEnd()
          $reader.Close()
        }
        $res.Ok = ($res.Status -ge 200 -and $res.Status -lt 300)
      }
    } catch { }
  }

  return [pscustomobject]$res
}

function Try-ParseJson($text) {
  if ([string]::IsNullOrWhiteSpace($text)) { return $null }
  try { return ($text | ConvertFrom-Json -ErrorAction Stop) } catch { return $null }
}

function Find-FirstPath($swagger, [string[]]$patterns) {
  # patterns are regex snippets like 'register|signup'
  if ($swagger -eq $null) { return $null }
  if ($swagger.paths -eq $null) { return $null }

  $keys = @()
  foreach ($p in $swagger.paths.PSObject.Properties) { $keys += $p.Name }

  foreach ($pat in $patterns) {
    foreach ($k in $keys) {
      if ($k -match $pat) { return $k }
    }
  }
  return $null
}

function Find-AnyPaths($swagger, [string[]]$patterns) {
  $out = @()
  if ($swagger -eq $null -or $swagger.paths -eq $null) { return $out }
  foreach ($p in $swagger.paths.PSObject.Properties) {
    $k = $p.Name
    foreach ($pat in $patterns) {
      if ($k -match $pat) { $out += $k; break }
    }
  }
  return ($out | Select-Object -Unique)
}

function Extract-Token($loginJson) {
  if ($loginJson -eq $null) { return $null }
  # common names:
  foreach ($name in @("token","Token","accessToken","AccessToken","jwt","Jwt","jwtToken","JwtToken")) {
    try {
      $v = $loginJson.$name
      if ($v -and $v.Length -gt 10) { return $v }
    } catch {}
  }
  # sometimes token is nested
  try {
    if ($loginJson.data -and $loginJson.data.token) { return $loginJson.data.token }
  } catch {}
  return $null
}

function New-RandomEmail() {
  return ("ps1_{0}@example.com" -f ([guid]::NewGuid().ToString("N").Substring(0,12)))
}

# =========================
# 0) Swagger
# =========================
Write-Section "0) Swagger"
$swaggerUrl = "$BaseUrl/swagger/v1/swagger.json"
$swaggerResp = Invoke-Api -Method GET -Url $swaggerUrl

if (-not $swaggerResp.Ok) {
  Write-Fail ("Swagger FAIL. Status={0} Err={1}" -f $swaggerResp.Status, $swaggerResp.Error)
  if ($swaggerResp.Content) { Write-Host $swaggerResp.Content }
  throw "Swagger unavailable"
}
Write-OK ("Swagger OK (Status {0})" -f $swaggerResp.Status)
$swagger = Try-ParseJson $swaggerResp.Content
if ($swagger -eq $null) { Write-Warn "Nie uda³o siê sparsowaæ swagger.json jako JSON (ale 200 OK). Testy bêd¹ mniej 'smart'." }

# Detect common endpoints
$RegisterPath = Find-FirstPath $swagger @("register","signup","auth\/register","users\/register")
$LoginPath    = Find-FirstPath $swagger @("login","signin","auth\/login","users\/login")
$CurrentPath  = Find-FirstPath $swagger @("current","me","session","auth\/me","sessions\/current")
$BoardsPaths  = Find-AnyPaths  $swagger @("kanban","board","boards","kanbanboard")
$SkillsPaths  = Find-AnyPaths  $swagger @("skill","skills","userskill")
$MatchPaths   = Find-AnyPaths  $swagger @("match","swipe","suggest","recommend")

if (-not $RegisterPath) { $RegisterPath = "/api/Auth/register" }
if (-not $LoginPath)    { $LoginPath    = "/api/Auth/login" }

Write-Host ("RegisterPath: {0}" -f ($RegisterPath))
Write-Host ("LoginPath   : {0}" -f ($LoginPath))
Write-Host ("CurrentPath : {0}" -f ($(if ($CurrentPath) { $CurrentPath } else { "<none>" })))
if ($BoardsPaths.Count -gt 0) { Write-Host ("Boards paths: {0}" -f ($BoardsPaths -join ", ")) } else { Write-Host "Boards paths: <none found>" }
if ($SkillsPaths.Count -gt 0) { Write-Host ("Skills paths: {0}" -f ($SkillsPaths -join ", ")) } else { Write-Host "Skills paths: <none found>" }
if ($MatchPaths.Count -gt 0) { Write-Host ("Match/Suggest paths: {0}" -f ($MatchPaths -join ", ")) } else { Write-Host "Match/Suggest paths: <none found>" }

# =========================
# 1) Register (User A)
# =========================
Write-Section "1) Register (User A)"

$emailA = New-RandomEmail
$passA  = "wsbb12"   # >=6 znaków
$regA = Invoke-Api -Method POST -Url ($BaseUrl + $RegisterPath) -Body (To-JsonBody @{
  firstName = "PS1"
  lastName  = "UserA"
  email     = $emailA
  password  = $passA
})

if (-not $regA.Ok) {
  Write-Fail ("Register A FAIL: Status={0} Err={1}" -f $regA.Status, $regA.Error)
  if ($regA.Content) { Write-Host $regA.Content }
  throw "Register A failed"
}
Write-OK ("Register A OK (Status {0}) email={1}" -f $regA.Status, $emailA)

# =========================
# 2) Login (User A)
# =========================
Write-Section "2) Login (User A)"

$loginA = Invoke-Api -Method POST -Url ($BaseUrl + $LoginPath) -Body (To-JsonBody @{
  email    = $emailA
  password = $passA
})

if (-not $loginA.Ok) {
  Write-Fail ("Login A FAIL: Status={0} Err={1}" -f $loginA.Status, $loginA.Error)
  if ($loginA.Content) { Write-Host $loginA.Content }
  throw "Login A failed"
}
$loginAJson = Try-ParseJson $loginA.Content
$tokenA = Extract-Token $loginAJson
if (-not $tokenA) {
  Write-Warn "Nie znalaz³em tokena w odpowiedzi login. Sprawdzê czy API dzia³a bez JWT (mo¿e cookie)."
} else {
  Write-OK "Login A OK, token znaleziony."
}
$authHeadersA = @{}
if ($tokenA) { $authHeadersA["Authorization"] = "Bearer $tokenA" }

# =========================
# 3) Current session / Me (if exists)
# =========================
Write-Section "3) Current session / Me"
if ($CurrentPath) {
  $me = Invoke-Api -Method GET -Url ($BaseUrl + $CurrentPath) -Headers $authHeadersA
  if ($me.Ok) {
    Write-OK ("Me OK (Status {0})" -f $me.Status)
  } else {
    Write-Warn ("Me FAIL (Status {0}) Err={1}" -f $me.Status, $me.Error)
    if ($me.Content) { Write-Host $me.Content }
  }
} else {
  Write-Warn "Brak endpointu /me/current w Swaggerze — pomijam."
}

# =========================
# 4) Basic auth enforcement check on a protected endpoint (boards)
# =========================
Write-Section "4) Protected endpoint check (Boards)"
if ($BoardsPaths.Count -gt 0) {
  $boardsPath = $BoardsPaths[0]
  # without token
  $noAuth = Invoke-Api -Method GET -Url ($BaseUrl + $boardsPath)
  if ($noAuth.Status -eq 401 -or $noAuth.Status -eq 403) {
    Write-OK ("GET bez tokena poprawnie zablokowany: Status={0}" -f $noAuth.Status)
  } else {
    Write-Warn ("GET bez tokena nie zwróci³ 401/403 (Status={0})" -f $noAuth.Status)
  }

  # with token
  if ($tokenA) {
    $withAuth = Invoke-Api -Method GET -Url ($BaseUrl + $boardsPath) -Headers $authHeadersA
    if ($withAuth.Ok) {
      Write-OK ("GET z tokenem OK: Status={0}" -f $withAuth.Status)
    } else {
      Write-Warn ("GET z tokenem FAIL: Status={0} Err={1}" -f $withAuth.Status, $withAuth.Error)
      if ($withAuth.Content) { Write-Host $withAuth.Content }
    }
  } else {
    Write-Warn "Brak tokena — pomijam test GET z tokenem."
  }
} else {
  Write-Warn "Nie znalaz³em endpointu boards/kanban w Swaggerze."
}

# =========================
# 5) Try create a Kanban board (if POST exists)
# =========================
Write-Section "5) Kanban create/update/delete (best-effort)"
if ($BoardsPaths.Count -gt 0 -and $tokenA) {
  # Find a path with POST likely (shows up same path; swagger doesn't tell easily here)
  $boardCreatePath = $BoardsPaths | Where-Object { $_ -match "board" -or $_ -match "kanban" } | Select-Object -First 1
  if (-not $boardCreatePath) { $boardCreatePath = $BoardsPaths[0] }

  $createPayload = @{
    title = "PS1 Board " + (Get-Date -Format "HHmmss")
    description = "Board created by test-api-deep.ps1"
  }
  $create = Invoke-Api -Method POST -Url ($BaseUrl + $boardCreatePath) -Headers $authHeadersA -Body (To-JsonBody $createPayload)

  if ($create.Ok) {
    Write-OK ("POST board OK (Status {0})" -f $create.Status)
    $createJson = Try-ParseJson $create.Content
    $boardId = $null

    # Try to locate id
    foreach ($name in @("id","Id","boardId","BoardId")) {
      try {
        $v = $createJson.$name
        if ($v) { $boardId = $v; break }
      } catch {}
    }

    if ($boardId) {
      Write-OK ("BoardId detected: {0}" -f $boardId)

      # GET by id if swagger has /{id}
      $byIdPath = ($BoardsPaths | Where-Object { $_ -match "\{.*id.*\}" } | Select-Object -First 1)
      if ($byIdPath) {
        $url = ($BaseUrl + ($byIdPath -replace "\{.*id.*\}", $boardId))
        $getById = Invoke-Api -Method GET -Url $url -Headers $authHeadersA
        if ($getById.Ok) { Write-OK ("GET board by id OK (Status {0})" -f $getById.Status) }
        else { Write-Warn ("GET board by id FAIL Status={0}" -f $getById.Status) }
      } else {
        Write-Warn "Nie znalaz³em endpointu board/{id} w swagger — pomijam GET by id."
      }

      # DELETE best-effort
      if ($byIdPath) {
        $url = ($BaseUrl + ($byIdPath -replace "\{.*id.*\}", $boardId))
        $del = Invoke-Api -Method DELETE -Url $url -Headers $authHeadersA
        if ($del.Ok) { Write-OK ("DELETE board OK (Status {0})" -f $del.Status) }
        else { Write-Warn ("DELETE board FAIL Status={0}" -f $del.Status) }
      }
    } else {
      Write-Warn "Nie uda³o siê wykryæ ID z odpowiedzi create — pomijam testy GET/DELETE."
    }
  } else {
    Write-Warn ("POST board nieudany (Status {0}). To mo¿e byæ normalne jeœli endpoint wymaga innego payloadu (np. matchId)." -f $create.Status)
    if ($create.Content) { Write-Host $create.Content }
  }
} else {
  Write-Warn "Brak tokena albo brak boards path — pomijam CRUD."
}

# =========================
# 6) Matching / Suggestions (algorithm test - best-effort)
# =========================
Write-Section "6) Suggestions / Sorting algorithm (best-effort)"

if ($MatchPaths.Count -eq 0) {
  Write-Warn "Nie znalaz³em endpointów match/suggest w Swagger — nie da siê automatycznie przetestowaæ algorytmu bez wiedzy gdzie to jest."
} elseif (-not $tokenA) {
  Write-Warn "Brak tokena — pomijam sugestie."
} else {
  # Heuristic: pick something with suggest/recommend in path, else match
  $suggestPath = $MatchPaths | Where-Object { $_ -match "suggest|recommend" } | Select-Object -First 1
  if (-not $suggestPath) { $suggestPath = $MatchPaths | Select-Object -First 1 }

  Write-Host ("Spróbujê endpoint: {0}" -f $suggestPath)

  # Call suggestions multiple times to see stability / sorting behavior
  $runs = @()
  for ($i=1; $i -le 3; $i++) {
    $r = Invoke-Api -Method GET -Url ($BaseUrl + $suggestPath) -Headers $authHeadersA
    if (-not $r.Ok) {
      Write-Warn ("Sugestie run#{0} FAIL Status={1}" -f $i, $r.Status)
      if ($r.Content) { Write-Host $r.Content }
      break
    }

    $j = Try-ParseJson $r.Content
    if ($j -eq $null) {
      Write-Warn ("Sugestie run#{0} OK ale odpowiedŸ nie jest JSON" -f $i)
      break
    }

    # Try to interpret list
    $list = $null
    if ($j -is [System.Array]) { $list = $j }
    elseif ($j.items) { $list = $j.items }
    elseif ($j.data) { $list = $j.data }
    else { $list = $j }

    # Build signature of first 5 IDs/names to check ordering stability
    $sig = @()
    $count = 0
    foreach ($it in $list) {
      if ($count -ge 5) { break }
      $id = $null
      foreach ($n in @("id","Id","profileId","ProfileId","userId","UserId")) {
        try { if ($it.$n) { $id = $it.$n; break } } catch {}
      }
      if (-not $id) {
        try { if ($it.userName) { $id = $it.userName } } catch {}
      }
      if (-not $id) { $id = "[item]" }
      $sig += [string]$id
      $count++
    }

    $runs += ($sig -join ",")
    Write-OK ("Sugestie run#{0} OK. Top5: {1}" -f $i, ($sig -join ", "))
  }

  if ($runs.Count -ge 2) {
    if ($runs[0] -eq $runs[1]) { Write-OK "Sortowanie wygl¹da stabilnie (run1==run2 w top5)." }
    else { Write-Warn "Sortowanie ró¿ni siê miêdzy wywo³aniami (run1!=run2). To mo¿e byæ ok jeœli algorytm jest losowy albo zale¿y od czasu." }
  }
}

Write-Host ""
Write-OK "TESTY ZAKOÑCZONE"
exit 0
