# Variables
$versionFile = "version.txt"
$tokenFile = "ghcr-token.txt"
$dockerUser = "ayush1099"
$imageName = "weatherapp"

# Check token file
if (!(Test-Path $tokenFile)) {
    Write-Host "ERROR: $tokenFile not found." -ForegroundColor Red
    exit 1
}

# Read token
$TOKEN = (Get-Content $tokenFile | Out-String).Trim()

# Login to GHCR
Write-Host "Logging into GHCR..."
$TOKEN | docker login ghcr.io -u $dockerUser --password-stdin
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Login failed." -ForegroundColor Red
    exit 1
}

# Read current version
if (!(Test-Path $versionFile)) {
    "1.0" | Out-File $versionFile
}
$currentVersion = Get-Content $versionFile
$parts = $currentVersion.Split('.')
$major = [int]$parts[0]
$minor = [int]$parts[1]
$minor += 1
$newVersion = "$major.$minor"

Write-Host "Building Docker image version $newVersion ..."

# Build image
docker build -t ghcr.io/${dockerUser}/${imageName}:$newVersion .

# Tag as latest
docker tag ghcr.io/${dockerUser}/${imageName}:${newVersion} ghcr.io/${dockerUser}/${imageName}:latest

# Push both
docker push ghcr.io/${dockerUser}/${imageName}:${newVersion}
docker push ghcr.io/${dockerUser}/${imageName}:latest

# Update version.txt
${newVersion} | Out-File ${versionFile}

Write-Host "Done! New version is ${newVersion}" -ForegroundColor Green
