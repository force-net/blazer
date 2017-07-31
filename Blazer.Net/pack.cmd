C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe ..\Blazer.sln  /t:Rebuild /p:Configuration=Release /p:SolutionDir=%~dp0..\ 
dotnet build -c BuildCore -f .NETStandard1.3 project.json
"C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\sn.exe" -R bin\Release\Blazer.NET.dll ..\private.snk 
"C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\sn.exe" -R bin\BuildCore\netstandard1.3\Blazer.NET.dll ..\private.snk

..\.nuget\nuget.exe pack Blazer.Net.nuspec
xcopy *.nupkg _releases
del *.nupkg
