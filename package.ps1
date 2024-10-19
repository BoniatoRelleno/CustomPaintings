$output = "$PSScriptRoot/dist"
$publishPath = "$PSScriptRoot/publish"

if (Test-Path $output) {
    Remove-Item $output -Recurse
}

New-Item $output -ItemType Directory

dotnet publish "$PSScriptRoot/CustomPaintings/CustomPaintings.csproj" -o $publishPath

Copy-Item "$PSScriptRoot/publish/CustomPaintings.dll" -Destination $output
Copy-Item "$PSScriptRoot/package/*" -Destination $output
Copy-Item "$PSScriptRoot/CHANGELOG.md" -Destination $output

$resultFile = "$PSScriptRoot/CustomPaintings.zip"

if (Test-Path $resultFile) {
    Remove-Item $resultFile
}

Compress-Archive -Path "$output/*" -DestinationPath $resultFile
