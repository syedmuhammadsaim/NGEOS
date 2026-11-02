$desktopPath = [Environment]::GetFolderPath("Desktop")
$shortcutPath = Join-Path $desktopPath "NGEOS.lnk"
$vbsPath = "C:\Users\Saim\Desktop\NGEOS\Launch-NGEOS.vbs"
$iconPath = "C:\Users\Saim\Desktop\NGEOS\ngeos-desktop.ico"

$wshShell = New-Object -ComObject WScript.Shell
$shortcut = $wshShell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = "wscript.exe"
$shortcut.Arguments = "`"$vbsPath`""
$shortcut.WorkingDirectory = "C:\Users\Saim\Desktop\NGEOS"
$shortcut.IconLocation = "$iconPath,0"
$shortcut.Description = "NGEOS Humanitarian Platform - Desktop Application"
$shortcut.Save()

Write-Host "Updated Desktop Shortcut to completely silent launcher: $shortcutPath"
