src_path := src
docfx_config := doc/docfx.json
docfx_site_dir := doc/_site

header_formatting := \033[1m
command_formatting := \033[1;34m
desc_formatting := \033[0;32m
no_formatting := \033[0m

.PHONY: help test clean build

.DEFAULT_GOAL := help

## Show help for each of the Makefile recipes.
help:
	@printf "${header_formatting}Available targets:\n"
	@awk -F '## ' '/^## /{desc=$$2}/^[a-zA-Z0-9_-]+:/{gsub(/:.*/, "", $$1); printf "  ${command_formatting}%-20s ${desc_formatting}%s${no_formatting}\n", $$1, desc}' $(MAKEFILE_LIST) | sort
	@printf "\n"


## Verify code is ready for commit to branch, runs tests and verifies formatting.
verify: test
	@echo "Verifying code formatting..."
	dotnet format $(src_path) --verify-no-changes

## Does a dotnet clean
clean:
	dotnet clean $(src_path)

## Generate docfx site and serve, navigate to 127.0.0.1:8080
doc-serve:
	@echo "Generating DocFX site..."
	docfx $(docfx_config)
	@echo "Serving DocFX site at http://127.0.0.1:8080/ ..."
	docfx serve $(docfx_site_dir) -n 127.0.0.1

## Restores all dotnet projects
restore:
	dotnet restore $(src_path)

## Builds all the code
build:
	dotnet build $(src_path)

## Formats files using dotnet format
format:
	dotnet format $(src_path)

## Run all tests except Concordium integration
test:
	dotnet test $(src_path) --filter 'FullyQualifiedName!~ConcordiumIntegrationTests'

## Run all Unit-tests
unit-test:
	dotnet test $(src_path) --filter 'FullyQualifiedName!~IntegrationTests'

## Run Concordium integration tests, requires access to running node and environment variables
concordium-tests:
	dotnet test $(src_path)/ProjectOrigin.VerifiableEventStore.ConcordiumIntegrationTests
