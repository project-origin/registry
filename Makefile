src_path = src

default: help

help: # Show help for each of the Makefile recipes.
	@printf "Available targets:\n\n"
	@grep -E '^[a-zA-Z0-9 -]+:.*#'  Makefile | sort | while read -r l; do printf "  \033[1;32m$$(echo $$l | cut -f 1 -d':')\033[00m:$$(echo $$l | cut -f 2- -d'#')\n"; done
	@printf "\n"

verify: test # Verify code is ready for commit to branch, runs tests and verifies formatting.
	dotnet format $(src_path) --verify-no-changes

clean: # Does a dotnet clean
	dotnet clean $(src_path)

restore: # Restores all dotnet projectts
	dotnet restore $(src_path)

build: # Builds all the code
	dotnet build $(src_path)

format: # Formats files using dotnet format
	dotnet format $(src_path)

test: # Run all tests except Concordium integration
	dotnet test $(src_path) --filter 'FullyQualifiedName!~ConcordiumIntegrationTests'

unit-test: # Run all Unit-tests
	dotnet test $(src_path) --filter 'FullyQualifiedName!~IntegrationTests'

concordium-tests: # Run Concordium integration tests, requires access to running node and environment variables
	dotnet test $(src_path)/ProjectOrigin.VerifiableEventStore.ConcordiumIntegrationTests
