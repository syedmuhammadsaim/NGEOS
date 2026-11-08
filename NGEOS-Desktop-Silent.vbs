Set WshShell = CreateObject("WScript.Shell")
WshShell.CurrentDirectory = "C:\Users\Saim\Desktop\NGEOS"
WshShell.Run "cmd.exe /c ""C:\Users\Saim\Desktop\NGEOS\NGEOS-Desktop-Launcher.bat""", 0, False
