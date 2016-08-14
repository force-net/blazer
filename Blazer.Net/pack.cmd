C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe /t:Rebuild /p:Configuration=Release /p:SolutionDir=%~dp0\..\
..\.nuget\nuget.exe pack Blazer.Net.nuspec
xcopy *.nupkg _releases
del *.nupkg
