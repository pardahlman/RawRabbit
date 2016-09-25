echo "test: Test started"

Push-Location $PSScriptRoot

foreach ($test in ls ../test/*) {
	Push-Location $test

	echo "build: Testing project in $test"

	& dotnet test -c Release
	if($LASTEXITCODE -ne 0) { exit 3 }

	Pop-Location
}

Pop-Location