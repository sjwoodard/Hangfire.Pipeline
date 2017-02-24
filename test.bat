rmdir output /s /q
mkdir output

dotnet build -c Debug test/*/project.json

"%userprofile%\.nuget\packages\OpenCover\4.6.519\tools\OpenCover.Console.exe" ^
	-target:"%userprofile%\.nuget\packages\NUnit.ConsoleRunner\3.4.1\tools\nunit3-console.exe" ^
	-targetargs:"--result output\nunit.xml test\Hangfire.Pipeline.Tests\bin\Debug\net45\Hangfire.Pipeline.Tests.dll test\Hangfire.Pipeline.SqlServer.Tests\bin\Debug\net45\Hangfire.Pipeline.SqlServer.Tests.dll test\Hangfire.Pipeline.Windsor.Tests\bin\Debug\net45\Hangfire.Pipeline.Windsor.Tests.dll" ^
	-filter:"+[Hangfire.Pipeline*]* -[*.Tests]*" ^
	-register:user ^
	-output:"output\opencover.xml"

"%userprofile%\.nuget\packages\ReportGenerator\2.4.5\tools\ReportGenerator.exe" ^
	"-reports:output\opencover.xml" ^
	"-targetdir:output\report"