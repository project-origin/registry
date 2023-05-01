src_path := src

docfx_config := doc/docfx.json
docfx_site_dir := doc/_site

formatting_header := \033[1m
formatting_command := \033[1;34m
formatting_desc := \033[0;32m
formatting_none := \033[0m

.PHONY: help test clean build

.DEFAULT_GOAL := help

## Show help for each of the Makefile recipes.
help:

	@printf "${formatting_header}Available targets:\n"
	@awk -F '## ' '/^## /{desc=$$2}/^[a-zA-Z0-9_-]+:/{gsub(/:.*/, "", $$1); printf "  ${formatting_command}%-20s ${formatting_desc}%s${formatting_none}\n", $$1, desc}' $(MAKEFILE_LIST) | sort
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
	docfx build $(docfx_config)
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
