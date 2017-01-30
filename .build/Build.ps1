write-host "build: Build started" -ForegroundColor Green

Push-Location $PSScriptRoot

if(Test-Path ..\artifacts) {
	write-host "build: Cleaning .\artifacts" -ForegroundColor Green
	Remove-Item ..\artifacts -Force -Recurse
}

& dotnet restore ../ --no-cache

$branch = @{ $true = $env:APPVEYOR_REPO_BRANCH; $false = $(git symbolic-ref --short -q HEAD) }[$env:APPVEYOR_REPO_BRANCH -ne $NULL];
$revision = @{ $true = "{0:00000}" -f [convert]::ToInt32("0" + $env:APPVEYOR_BUILD_NUMBER, 10); $false = "local" }[$env:APPVEYOR_BUILD_NUMBER -ne $NULL];
$suffix = @{ $true = ""; $false = "$($branch.Substring(0, [math]::Min(10,$branch.Length)))-$revision"}[$branch -eq "master" -and $revision -ne "local"]

write-host "build: Version suffix is $suffix" -ForegroundColor Green

foreach ($src in ls ../src/*) {
	Push-Location $src

	write-host "build: Packaging project in $src" -ForegroundColor Green

	& dotnet pack -c Release -o ..\..\artifacts --version-suffix="beta1"
	if($LASTEXITCODE -ne 0) { exit 1 }

	Pop-Location
}

Pop-Location