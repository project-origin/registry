ARG VARIANT="6.0-bullseye-slim"
FROM mcr.microsoft.com/vscode/devcontainers/dotnet:${VARIANT}

HEALTHCHECK CMD curl --fail http://localhost:8080/health || exit 1
