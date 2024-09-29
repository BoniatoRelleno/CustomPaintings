$output = "$PSScriptRoot/dist"
$publishPath = "$PSScriptRoot/publish"

if (Test-Path $output) {
    Remove-Item $output -Recurse
}

New-Item $output -ItemType Directory

dotnet publish "$PSScriptRoot/CustomPainting/CustomPainting.csproj" -o $publishPath

Copy-Item "$PSScriptRoot/publish/CustomPainting.dll" -Destination $output
Copy-Item "$PSScriptRoot/package/*" -Destination $output
Copy-Item "$PSScriptRoot/README.md" -Destination $output
Copy-Item "$PSScriptRoot/CHANGELOG.md" -Destination $output

Compress-Archive -Path "$output/*" -DestinationPath "$PSScriptRoot/CustomPainting.zip"