C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe /t:Rebuild /p:Configuration=Release /p:Platform=x64 "/p:VCTargetsPath=C:\Program Files (x86)\MSBuild\Microsoft.Cpp\v4.0\V110\"
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe /t:Rebuild /p:Configuration=Release /p:Platform=Win32 "/p:VCTargetsPath=C:\Program Files (x86)\MSBuild\Microsoft.Cpp\v4.0\V110\"
xcopy /y Release\Blazer.Native.x86.dll ..\Blazer.Native.Build\
xcopy /y Release\Blazer.Native.x64.dll ..\Blazer.Native.Build\

del /s /q  %temp%\Blazer.Net.0.0.1.0