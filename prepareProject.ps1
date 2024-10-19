$gamePath = "C:/Program Files (x86)/Steam/steamapps/common/Lethal Company";
$csproj = "$PSScriptRoot/CustomPaintings/CustomPaintings.csproj";

$systemIsWindows = $IsWindows ?? ($Env:OS.Contains(("Windows")));

$folderSelectPromt = "";

if ($systemIsWindows) {
    $folderSelectPromt = " / folderselect";
}

$message = "Please select Lethal Company game folder [""$gamePath""$folderSelectPromt]"

$userInput = Read-Host $message

if ($systemIsWindows -and $userInput -eq "folderselect") {
    Add-Type -AssemblyName System.Windows.Forms
    $folderBrowser = New-Object System.Windows.Forms.FolderBrowserDialog;
    if ($folderBrowser.ShowDialog() -eq [System.Windows.Forms.DialogResult]::Cancel) {
        Write-Output "User cancelled selection"
        exit
    }
    $gamePath = $folderBrowser.SelectedPath;
} elseif ($userInput -ne "") {
    $gamePath = $userInput
}

$projectXml = [xml](Get-Content $csproj);

$gameReferences = $projectXml.Project.ItemGroup | Where-Object { $_.Label -eq "GameAssetsReferences" }

foreach ($reference in $gameReferences.Reference) {
    $index = $reference.HintPath.LastIndexOf("Lethal Company\");
    if ($index -lt 0) {
        $index = $reference.HintPath.LastIndexOf("Lethal Company/")
    }
    if ($index -lt 0) {
        $index = 0;
    } else {
        $index = $index + "Lethal Company\".Length;
    }
    $file = $reference.HintPath.Substring($index)
    $reference.HintPath = [System.IO.Path]::Combine($gamePath, $file)
}

$projName = [System.IO.Path]::GetFileNameWithoutExtension($csproj)

$projectXml.Project.PropertyGroup.OutputPath = [System.IO.Path]::Combine($gamePath, "BepInEx/plugins/$projName")

$projectXml.Save($csproj)
