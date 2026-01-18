param(
  [string]$BaseUrl = "https://localhost:7291",
  [int]$SkillId = 1,
  [switch]$Insecure
)

# =========================
# Setup
# =========================
$ErrorActionPreference = "Stop"

if ($Insecure) {
  # Local-dev only: accept self-signed cert for localhost
  [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
  [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
}

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

  $res = [ordered]@{
    Ok = $false
    Status = $null
    Content = $null
    Error = $null
    Headers = $null
  }

  try {
    $params = @{
      Method        = $Method
      Uri           = $Url
      TimeoutSec    = $TimeoutSec
      ErrorAction   = "Stop"
      UseBasicParsing = $true   # PS5.1: usuwa te "Script Execution Risk" prompty
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

function New-RandomEmail() {
  return ("seed_{0}@example.com" -f ([guid]::NewGuid().ToString("N").Substring(0,12)))
}

function Extract-Token($loginJson) {
  if ($loginJson -eq $null) { return $null }
  foreach ($name in @("token","Token","accessToken","AccessToken","jwt","Jwt","jwtToken","JwtToken")) {
    try {
      $v = $loginJson.$name
      if ($v -and $v.Length -gt 10) { return $v }
    } catch {}
  }
  try {
    if ($loginJson.data -and $loginJson.data.token) { return $loginJson.data.token }
  } catch {}
  return $null
}

function Find-Path($swagger, [string[]]$patterns) {
  if ($swagger -eq $null -or $swagger.paths -eq $null) { return $null }
  $keys = @()
  foreach ($p in $swagger.paths.PSObject.Properties) { $keys += $p.Name }
  foreach ($pat in $patterns) {
    foreach ($k in $keys) {
      if ($k -match $pat) { return $k }
    }
  }
  return $null
}

function Find-AllPaths($swagger, [string[]]$patterns) {
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

function Has-Method($swagger, [string]$path, [string]$methodLower) {
  if ($swagger -eq $null -or $swagger.paths -eq $null) { return $false }
  $p = ($swagger.paths.PSObject.Properties | Where-Object { $_.Name -eq $path } | Select-Object -First 1)
  if (-not $p) { return $false }
  $ops = $p.Value
  $names = @()
  foreach ($x in $ops.PSObject.Properties) { $names += $x.Name.ToLowerInvariant() }
  return ($names -contains $methodLower.ToLowerInvariant())
}

# =========================
# 0) Swagger + endpoints
# =========================
Write-Section "0) Swagger"
$swaggerUrl = "$BaseUrl/swagger/v1/swagger.json"
$sw = Invoke-Api -Method GET -Url $swaggerUrl
if (-not $sw.Ok) { Write-Fail "Swagger FAIL"; if ($sw.Content) {Write-Host $sw.Content}; throw "Swagger unavailable" }
Write-OK "Swagger OK"
$swagger = Try-ParseJson $sw.Content

$RegisterPath = Find-Path $swagger @("session\/register","auth\/register","register")
$LoginPath    = Find-Path $swagger @("session\/login","auth\/login","login")
$SuggestPath  = Find-Path $swagger @("match\/suggestions","suggestions")
$UserSkillMe  = Find-Path $swagger @("userskill\/me")
$ProfilePaths = Find-AllPaths $swagger @("profile")

if (-not $RegisterPath) { $RegisterPath = "/api/Session/register" }
if (-not $LoginPath)    { $LoginPath    = "/api/Session/login" }
if (-not $SuggestPath)  { $SuggestPath  = "/api/Match/suggestions" }

# heurystyka: profil "me/current" z PUT
$ProfilePut = $null
if ($ProfilePaths.Count -gt 0) {
  $ProfilePut = $ProfilePaths |
    Where-Object { $_ -match "me|current" } |
    Where-Object { Has-Method $swagger $_ "put" } |
    Select-Object -First 1

  if (-not $ProfilePut) {
    $ProfilePut = $ProfilePaths |
      Where-Object { Has-Method $swagger $_ "put" } |
      Select-Object -First 1
  }
}

Write-Host ("RegisterPath : {0}" -f $RegisterPath)
Write-Host ("LoginPath    : {0}" -f $LoginPath)
Write-Host ("SuggestPath  : {0}" -f $SuggestPath)
Write-Host ("UserSkill/me : {0}" -f ($(if ($UserSkillMe) { $UserSkillMe } else { "<not found>" })))
Write-Host ("Profile PUT  : {0}" -f ($(if ($ProfilePut) { $ProfilePut } else { "<not found>" })))

# =========================
# Helpers: register/login + profile + userSkill
# =========================
function Register-And-Login([string]$label) {
  $email = New-RandomEmail
  $pass  = "wsbb12"

  Write-Section "Register $label"
  $reg = Invoke-Api -Method POST -Url ($BaseUrl + $RegisterPath) -Body (To-JsonBody @{
    firstName = "Seed"
    lastName  = $label
    email     = $email
    password  = $pass
  })
  if (-not $reg.Ok) { Write-Fail "Register fail"; if ($reg.Content){Write-Host $reg.Content}; throw "Register failed" }
  Write-OK ("Registered {0} ({1})" -f $label, $email)

  Write-Section "Login $label"
  $log = Invoke-Api -Method POST -Url ($BaseUrl + $LoginPath) -Body (To-JsonBody @{
    email    = $email
    password = $pass
  })
  if (-not $log.Ok) { Write-Fail "Login fail"; if ($log.Content){Write-Host $log.Content}; throw "Login failed" }

  $json = Try-ParseJson $log.Content
  $token = Extract-Token $json
  if (-not $token) { throw "No token in login response. SprawdŸ response login w Swagger." }

  Write-OK "Token OK"
  return @{
    Email = $email
    Token = $token
    Headers = @{ Authorization = "Bearer $token" }
  }
}

function Ensure-Onboarding([hashtable]$auth, [string]$userName) {
  if (-not $ProfilePut) {
    Write-Warn "Nie znalaz³em endpointu Profile PUT w Swagger. Ustaw IsOnboardingComplete=1 rêcznie w DB albo podeœlij swagger Profile."
    return
  }

  # payload "bezpieczny": jeœli Twoje DTO ma wymagane pola, dopisz je tutaj wg Swagger.
  $payload = @{
    userName = $userName
    bio = "Seed profile"
    country = "PL"
    preferredMeetingType = 0
    preferredLearningStyle = 0
    availability = 7
    isOnboardingComplete = $true
  }

  $r = Invoke-Api -Method PUT -Url ($BaseUrl + $ProfilePut) -Headers $auth.Headers -Body (To-JsonBody $payload)

  if ($r.Ok) {
    Write-OK "Profile updated (onboarding complete)"
    return
  }

  Write-Warn ("Profile PUT failed (Status {0}). Spróbujê minimalny payload tylko isOnboardingComplete..." -f $r.Status)

  $payload2 = @{ isOnboardingComplete = $true }
  $r2 = Invoke-Api -Method PUT -Url ($BaseUrl + $ProfilePut) -Headers $auth.Headers -Body (To-JsonBody $payload2)
  if ($r2.Ok) {
    Write-OK "Profile updated (minimal payload)"
  } else {
    Write-Warn ("Profile PUT nadal nie dzia³a (Status {0}). Wypisujê body:" -f $r2.Status)
    if ($r2.Content) { Write-Host $r2.Content }
  }
}

function Add-UserSkill-Me([hashtable]$auth, [int]$skillId, [bool]$learned, [int]$level = 4) {
  if (-not $UserSkillMe) {
    Write-Warn "Nie znalaz³em /api/UserSkill/me w Swagger. Dodaj skill rêcznie albo dopasuj endpoint."
    return
  }

  $payload = @{
    skillId = $skillId
    learned = $learned
    level = $level
  }

  $r = Invoke-Api -Method POST -Url ($BaseUrl + $UserSkillMe) -Headers $auth.Headers -Body (To-JsonBody $payload)
  if ($r.Ok) { Write-OK ("UserSkill added (skillId={0}, learned={1})" -f $skillId, $learned) }
  else {
    Write-Warn ("UserSkill POST failed (Status {0})" -f $r.Status)
    if ($r.Content) { Write-Host $r.Content }
  }
}

# =========================
# 1) Create two users: Teacher + Learner
# =========================
$teacher = Register-And-Login "Teacher"
$learner = Register-And-Login "Learner"

# 2) Ensure profiles are eligible (onboarding complete)
Write-Section "Profiles"
Ensure-Onboarding $teacher "Teacher_seed"
Ensure-Onboarding $learner "Learner_seed"

# 3) Add skills: teacher teaches, learner learns
Write-Section "UserSkills"
Add-UserSkill-Me $teacher $SkillId $true  4
Add-UserSkill-Me $learner $SkillId $false 1

# 4) Call suggestions as learner
Write-Section "Suggestions (as Learner)"
$s = Invoke-Api -Method GET -Url ($BaseUrl + $SuggestPath) -Headers $learner.Headers
if ($s.Ok) {
  Write-OK "Suggestions OK"
  Write-Host $s.Content
} else {
  Write-Warn ("Suggestions FAIL (Status {0})" -f $s.Status)
  if ($s.Content) { Write-Host $s.Content }
}

Write-Section "DONE"
Write-OK "Seed completed"
