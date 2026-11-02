Option Explicit

Dim fso, wshShell, msedgePath, chromePath, browserPath, appUrl, isRunning, i
Set fso = CreateObject("Scripting.FileSystemObject")
Set wshShell = CreateObject("WScript.Shell")

' 1. Detect Microsoft Edge or Google Chrome
msedgePath = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
If Not fso.FileExists(msedgePath) Then
    msedgePath = "C:\Program Files\Microsoft\Edge\Application\msedge.exe"
End If

chromePath = "C:\Program Files\Google\Chrome\Application\chrome.exe"
If Not fso.FileExists(chromePath) Then
    chromePath = "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"
End If

If fso.FileExists(msedgePath) Then
    browserPath = msedgePath
ElseIf fso.FileExists(chromePath) Then
    browserPath = chromePath
Else
    browserPath = ""
End If

' 2. Helper function to test HTTP status without errors
Function CheckUrl(url)
    Dim http
    On Error Resume Next
    Set http = CreateObject("MSXML2.ServerXMLHTTP.6.0")
    http.setTimeouts 400, 400, 400, 400
    http.Open "GET", url, False
    http.Send
    If Err.Number = 0 Then
        CheckUrl = True
    Else
        CheckUrl = False
    End If
    Err.Clear
    On Error GoTo 0
End Function

' 3. Check if server is already running on port 5125 (Visual Studio) or port 5000
isRunning = False
If CheckUrl("http://localhost:5125/manifest.json") Then
    isRunning = True
    appUrl = "http://localhost:5125"
ElseIf CheckUrl("http://localhost:5000/manifest.json") Then
    isRunning = True
    appUrl = "http://localhost:5000"
End If

' 4. If not running, start backend completely SILENT in background (WindowStyle = 0, no black box!)
If Not isRunning Then
    appUrl = "http://localhost:5000"
    Dim publishExe, rootDir
    rootDir = "C:\Users\Saim\Desktop\NGEOS"
    publishExe = rootDir & "\publish\NGEOS.exe"
    
    If fso.FileExists(publishExe) Then
        wshShell.CurrentDirectory = rootDir & "\publish"
        ' Run NGEOS.exe silently in background
        wshShell.Run """" & publishExe & """", 0, False
    Else
        wshShell.CurrentDirectory = rootDir & "\NGEOS"
        wshShell.Run "dotnet run --launch-profile http", 0, False
    End If
    
    ' Wait until server responds to HTTP requests (up to 15 seconds)
    For i = 1 To 30
        WScript.Sleep 500
        If CheckUrl("http://localhost:5000/manifest.json") Then
            Exit For
        End If
    Next
End If

' 5. Launch frameless Desktop App Window directly
If browserPath <> "" Then
    wshShell.Run """" & browserPath & """ --app=""" & appUrl & """ --window-size=1366,850", 1, False
Else
    wshShell.Run appUrl, 1, False
End If
