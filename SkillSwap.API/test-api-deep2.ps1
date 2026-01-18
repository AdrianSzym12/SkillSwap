param(
  [string]$BaseUrl = "https://localhost:7291",
  [int]$MatchId = 0
)

$ErrorActionPreference = "Stop"

function Write-Section([string]$t) { Write-Host ""; Write-Host ("=== {0} ===" -f $t) }
function Write-OK([string]$t)      { Write-Host ("[OK]  {0}" -f $t) }
function Write-Warn([string]$t)    { Write-Host ("[WARN] {0}" -f $t) }
function Write-Fail([string]$t)    { Write-Host ("[FAIL] {0}" -f $t) }

function To-JsonBody($obj) { return ($obj | ConvertTo-Json -Depth 20) }

function Invoke-Api {
  param(
    [ValidateSet("GET","POST","PUT","PATCH","DELETE")]
    [string]$Method,
    [string]$Url,
    [hashtable]$Headers = $null,
    $Body = $null,
    [int]$TimeoutSec = 30
  )

  $res = [ordered]@{ Ok=$false; Status=$null; Content=$null; Error=$null; Headers=$null }

  try {
    $params = @{
      Method      = $Method
      Uri         = $Url
      TimeoutSec  = $TimeoutSec
      ErrorAction = "Stop"
      UseBasicParsing = $true   # usuwa prompt "Script Execution Risk"
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

function Get-PathObj($swagger, [string]$path) {
  if ($swagger -eq $null -or $swagger.paths -eq $null) { return $null }
  foreach ($p in $swagger.paths.PSObject.Properties) {
    if ($p.Name -eq $path) { return $p.Value }
  }
  return $null
}

function Has-Verb($swagger, [string]$path, [string]$verb) {
  $obj = Get-PathObj $swagger $path
  if ($obj -eq $null) { return $false }
  try { return ($obj.PSObject.Properties.Name -contains $verb) } catch { return $false }
}

function Find-FirstPath($swagger, [string[]]$patterns) {
  if ($swagger -eq $null -or $swagger.paths -eq $null) { return $null }
  $keys = @()
  foreach ($p in $swagger.paths.PSObject.Properties) { $keys += $p.Name }
  foreach ($pat in $patterns) {
    foreach ($k in $keys) { if ($k -match $pat) { return $k } }
  }
  return $null
}

function Find-AnyPaths($swagger, [string[]]$patterns) {
  $out = @()
  if ($swagger -eq $null -or $swagger.paths -eq $null) { return $out }
  foreach ($p in $swagger.paths.PSObject.Properties) {
    $k = $p.Name
    foreach ($pat in $patterns) { if ($k -match $pat) { $out += $k; break } }
  }
  return ($out | Select-Object -Unique)
}

function Extract-Token($loginJson) {
  if ($loginJson -eq $null) { return $null }
  foreach ($name in @("token","Token","accessToken","AccessToken","jwt","Jwt","jwtToken","JwtToken")) {
    try { $v = $loginJson.$name; if ($v -and $v.Length -gt 10) { return $v } } catch {}
  }
  try { if ($loginJson.data -and $loginJson.data.token) { return $loginJson.data.token } } catch {}
  return $null
}

function New-RandomEmail() {
  return ("ps1_{0}@example.com" -f ([guid]::NewGuid().ToString("N").Substring(0,12)))
}

function Find-AnyId($obj) {
  if ($obj -eq $null) { return $null }
  foreach ($name in @("id","Id","boardId","BoardId","matchId","MatchId")) {
    try { $v = $obj.$name; if ($v) { return $v } } catch {}
  }
  try { if ($obj.data) { return (Find-AnyId $obj.data) } } catch {}
  return $null
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
if ($swagger -eq $null) { Write-Warn "Nie uda³o siê sparsowaæ swagger.json jako JSON (ale 200 OK)." }

$RegisterPath = Find-FirstPath $swagger @("session\/register","register","signup","auth\/register","users\/register")
$LoginPath    = Find-FirstPath $swagger @("session\/login","login","signin","auth\/login","users\/login")
$CurrentPath  = Find-FirstPath $swagger @("session\/current","current","me","auth\/me")

if (-not $RegisterPath) { $RegisterPath = "/api/Session/register" }
if (-not $LoginPath)    { $LoginPath    = "/api/Session/login" }

# Kanban paths 
$BoardsListPath = "/api/KanbanBoard"
$BoardByIdPath  = "/api/KanbanBoard/{id}"
$BoardByMatchPath = "/api/KanbanBoard/match/{matchId}"

# Match suggestions/my
$MatchMyPath = "/api/Match/my"
$SuggestPath = "/api/Match/suggestions"

Write-Host ("RegisterPath: {0}" -f $RegisterPath)
Write-Host ("LoginPath   : {0}" -f $LoginPath)
Write-Host ("CurrentPath : {0}" -f ($(if ($CurrentPath) { $CurrentPath } else { "<none>" })))
Write-Host ("BoardsList  : {0}" -f $BoardsListPath)
Write-Host ("BoardById   : {0}" -f $BoardByIdPath)
Write-Host ("BoardByMatch: {0}" -f $BoardByMatchPath)
Write-Host ("MatchMy     : {0}" -f $MatchMyPath)
Write-Host ("Suggestions : {0}" -f $SuggestPath)

# =========================
# 1) Register
# =========================
Write-Section "1) Register (User A)"
$emailA = New-RandomEmail
$passA  = "wsbb12"

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
# 2) Login
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
if (-not $tokenA) { Write-Warn "Nie znalaz³em tokena w odpowiedzi login." }
else { Write-OK "Login A OK, token znaleziony." }

$authHeadersA = @{}
if ($tokenA) { $authHeadersA["Authorization"] = "Bearer $tokenA" }

# =========================
# 3) Current / Me (tylko jeœli swagger mówi, ¿e GET istnieje)
# =========================
Write-Section "3) Current session / Me"
if ($CurrentPath -and (Has-Verb $swagger $CurrentPath "get")) {
  $me = Invoke-Api -Method GET -Url ($BaseUrl + $CurrentPath) -Headers $authHeadersA
  if ($me.Ok) { Write-OK ("Me OK (Status {0})" -f $me.Status) }
  else {
    Write-Warn ("Me FAIL (Status {0}) Err={1}" -f $me.Status, $me.Error)
    if ($me.Content) { Write-Host $me.Content }
  }
} else {
  Write-Warn "Pominiêto /current (Swagger nie pokazuje GET albo brak endpointu) — ¿eby nie waliæ 405."
}

# =========================
# 4) Protected endpoint check (Boards LIST, bez {id})
# =========================
Write-Section "4) Protected endpoint check (Boards LIST)"
if ($BoardsListPath -and (Has-Verb $swagger $BoardsListPath "get")) {
  $noAuth = Invoke-Api -Method GET -Url ($BaseUrl + $BoardsListPath)
  if ($noAuth.Status -eq 401 -or $noAuth.Status -eq 403) {
    Write-OK ("GET bez tokena poprawnie zablokowany: Status={0}" -f $noAuth.Status)
  } else {
    Write-Warn ("GET bez tokena nie zwróci³ 401/403 (Status={0})" -f $noAuth.Status)
  }

  if ($tokenA) {
    $withAuth = Invoke-Api -Method GET -Url ($BaseUrl + $BoardsListPath) -Headers $authHeadersA
    if ($withAuth.Ok) { Write-OK ("GET z tokenem OK: Status={0}" -f $withAuth.Status) }
    else {
      Write-Warn ("GET z tokenem FAIL: Status={0} Err={1}" -f $withAuth.Status, $withAuth.Error)
      if ($withAuth.Content) { Write-Host $withAuth.Content }
    }
  }
} else {
  Write-Warn "Brak GET na /api/KanbanBoard w Swaggerze — pomijam."
}

# =========================
# 5) KanbanBoard create/delete (z MatchId)
# =========================
Write-Section "5) KanbanBoard create/delete (best-effort)"

# spróbuj wyci¹gn¹æ MatchId automatycznie, jeœli nie podany
if ($MatchId -le 0 -and $tokenA) {
  $my = Invoke-Api -Method GET -Url ($BaseUrl + $MatchMyPath) -Headers $authHeadersA
  if ($my.Ok) {
    $myJson = Try-ParseJson $my.Content
    if ($myJson -ne $null) {
      # próba: lista lub data[]
      $list = $null
      if ($myJson -is [System.Array]) { $list = $myJson }
      elseif ($myJson.data) { $list = $myJson.data }
      else { $list = $myJson }

      try {
        if ($list -and $list.Count -gt 0) {
          $maybe = Find-AnyId $list[0]
          if ($maybe) { $MatchId = [int]$maybe }
        }
      } catch {}
    }
  }
}

if ($MatchId -le 0) {
  Write-Warn "Nie mam MatchId. Podaj -MatchId X (bo Create KanbanBoard u Ciebie wymaga MatchId). Pomijam POST/DELETE."
}
elseif (-not $tokenA) {
  Write-Warn "Brak tokena — pomijam CRUD."
}
elseif (-not (Has-Verb $swagger $BoardsListPath "post")) {
  Write-Warn "Swagger nie pokazuje POST na /api/KanbanBoard — pomijam."
}
else {
  $createPayload = @{
    matchId = $MatchId
    title = "PS1 Board " + (Get-Date -Format "HHmmss")
    description = "Board created by test-api-deep.ps1"
  }

  $create = Invoke-Api -Method POST -Url ($BaseUrl + $BoardsListPath) -Headers $authHeadersA -Body (To-JsonBody $createPayload)

  if (-not $create.Ok) {
    Write-Warn ("POST board FAIL (Status {0})." -f $create.Status)
    if ($create.Content) { Write-Host $create.Content }
  } else {
    Write-OK ("POST board OK (Status {0})" -f $create.Status)

    $createJson = Try-ParseJson $create.Content
    if ($createJson -eq $null) {
      Write-Warn "OdpowiedŸ create nie jest JSON — nie da siê wykryæ BoardId."
      if ($create.Content) { Write-Host ($create.Content.Substring(0, [Math]::Min(400, $create.Content.Length))) }
    } else {
      $boardId = Find-AnyId $createJson
      if (-not $boardId) {
        Write-Warn "Nie uda³o siê wykryæ boardId z odpowiedzi."
      } else {
        Write-OK ("BoardId detected: {0}" -f $boardId)

        # DELETE (jeœli swagger pokazuje delete na /{id})
        if (Has-Verb $swagger $BoardByIdPath "delete") {
          $delUrl = ($BaseUrl + ($BoardByIdPath -replace "\{id\}", $boardId))
          $del = Invoke-Api -Method DELETE -Url $delUrl -Headers $authHeadersA
          if ($del.Ok) { Write-OK ("DELETE board OK (Status {0})" -f $del.Status) }
          else {
            Write-Warn ("DELETE board FAIL Status={0}" -f $del.Status)
            if ($del.Content) { Write-Host $del.Content }
          }
        } else {
          Write-Warn "Swagger nie pokazuje DELETE na /api/KanbanBoard/{id} — pomijam."
        }
      }
    }
  }
}

# =========================
# 6) Suggestions (best-effort)
# =========================
Write-Section "6) Suggestions (best-effort)"
if (-not $tokenA) {
  Write-Warn "Brak tokena — pomijam sugestie."
}
elseif (-not (Has-Verb $swagger $SuggestPath "get")) {
  Write-Warn "Swagger nie pokazuje GET na /api/Match/suggestions — pomijam."
}
else {
  $r = Invoke-Api -Method GET -Url ($BaseUrl + $SuggestPath) -Headers $authHeadersA
  if (-not $r.Ok) {
    Write-Warn ("Sugestie FAIL Status={0}" -f $r.Status)
    if ($r.Content) { Write-Host $r.Content }
  } else {
    $j = Try-ParseJson $r.Content
    if ($j -eq $null) {
      Write-Warn "Sugestie OK, ale odpowiedŸ nie jest JSON (poka¿ê pocz¹tek):"
      if ($r.Content) { Write-Host ($r.Content.Substring(0, [Math]::Min(400, $r.Content.Length))) }
    } else {
      Write-OK "Sugestie OK (JSON)."
    }
  }
}

Write-Host ""
Write-OK "TESTY ZAKOÑCZONE"
exit 0
