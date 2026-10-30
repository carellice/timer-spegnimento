@echo off
rem compila TimerSpegnimento.exe con il compilatore C# incluso in Windows (.NET Framework 4)
cd /d "%~dp0"
rem icona dell'eseguibile: quella di spegnimento di Windows (shell32.dll, indice 27)
powershell -NoProfile -Command "Add-Type -AssemblyName System.Drawing; Add-Type -Namespace W -Name I -MemberDefinition '[DllImport(\"shell32.dll\",CharSet=CharSet.Unicode)] public static extern int ExtractIconEx(string f,int i,out IntPtr l,out IntPtr s,int n);'; $l=[IntPtr]::Zero; $s=[IntPtr]::Zero; [void][W.I]::ExtractIconEx('shell32.dll',27,[ref]$l,[ref]$s,1); $f=[IO.File]::Create(\"$env:TEMP\spegni.ico\"); [Drawing.Icon]::FromHandle($l).Save($f); $f.Close()"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /optimize /win32icon:"%TEMP%\spegni.ico" /out:TimerSpegnimento.exe TimerSpegnimento.cs
