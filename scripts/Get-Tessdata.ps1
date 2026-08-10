$ErrorActionPreference = "Stop"

$modelUrl = "https://github.com/tesseract-ocr/tessdata_fast/raw/refs/tags/4.1.0/eng.traineddata"
$expectedSha256 = "7d4322bd2a7749724879683fc3912cb542f19906c83bcc1a52132556427170b2"
$targetDirectory = Join-Path $PSScriptRoot "..\src\CrossOffLobbyDodger.Client\tessdata"
$targetPath = Join-Path $targetDirectory "eng.traineddata"
$temporaryPath = Join-Path ([System.IO.Path]::GetTempPath()) "CrossOffLobbyDodger-eng.traineddata"

New-Item -ItemType Directory -Force -Path $targetDirectory | Out-Null

if (Test-Path $targetPath) {
    $currentHash = (Get-FileHash $targetPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($currentHash -eq $expectedSha256) {
        Write-Host "Tesseract English model is already present and verified."
        exit 0
    }
}

try {
    Invoke-WebRequest -Uri $modelUrl -OutFile $temporaryPath
    $downloadHash = (Get-FileHash $temporaryPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($downloadHash -ne $expectedSha256) {
        throw "Tesseract model checksum mismatch. Expected $expectedSha256, received $downloadHash."
    }

    Move-Item -Force $temporaryPath $targetPath
    Write-Host "Downloaded and verified Tesseract English model."
}
finally {
    if (Test-Path $temporaryPath) {
        Remove-Item -Force $temporaryPath
    }
}
