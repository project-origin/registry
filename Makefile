src_path = src

default: restore format build unit-tests

verify: build
	dotnet format $(src_path) --verify-no-changes

clean:
	dotnet clean $(src_path)

restore:
	dotnet restore $(src_path)

build:
	dotnet build $(src_path)

format:
	dotnet format $(src_path)

unit-tests:
	dotnet test $(src_path) --filter 'FullyQualifiedName!~IntegrationTests'

concordium-tests:
	dotnet test $(src_path)/ProjectOrigin.VerifiableEventStore.ConcordiumIntegrationTests
