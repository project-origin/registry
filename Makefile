src_path = src

.PHONY: help test clean build

default: help

help: # Show help for each of the Makefile recipes.
	@printf "Available targets:\n\n"
	@grep -E '^[a-zA-Z0-9 -]+:.*#'  Makefile | sort | while read -r l; do printf "  \033[1;32m$$(echo $$l | cut -f 1 -d':')\033[00m:$$(echo $$l | cut -f 2- -d'#')\n"; done
	@printf "\n"

verify: test # Verify code is ready for commit to branch, runs tests and verifies formatting.
	dotnet format $(src_path) --verify-no-changes

clean: # Does a dotnet clean
	dotnet clean $(src_path)

doc-serve: # Generate docfx site and serve, navigate to 127.0.0.1:8080
	docfx doc/docfx.json
	docfx serve doc/_site -n 127.0.0.1

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

verify-chart: # Builds the local container, creates kind cluster and installs chart, and verifies it works
	@kind version >/dev/null 2>&1 || { echo >&2 "kind not installed! kind is required to use recipe, please install or use devcontainer"; exit 1;}
	@helm version >/dev/null 2>&1 || { echo >&2 "helm not installed! helm is required to use recipe, please install or use devcontainer"; exit 1;}

	kind delete cluster -n helm-test
	kind create cluster -n helm-test
	helm install cnpg-operator cloudnative-pg --repo https://cloudnative-pg.io/charts --version 0.18.0 --namespace cnpg --create-namespace --wait

	helm install wallet charts/project-origin-stack --set name=test --wait
	kind delete cluster -n helm-test
