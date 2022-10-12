default: restore build unit-tests

clean:
	dotnet clean src

restore:
	dotnet restore src

build:
	dotnet build src

unit-tests:
	dotnet test src --filter 'FullyQualifiedName!~IntegrationTests'

concordium-tests:
	dotnet test src/EnergyOrigin.VerifiableEventStore.ConcordiumIntegrationTests
