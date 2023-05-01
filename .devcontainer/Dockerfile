FROM mcr.microsoft.com/devcontainers/dotnet:7.0

# DocFX v2.66.2
USER vscode
RUN dotnet tool install --global docfx --version 2.66.2
ENV PATH "$PATH:/home/vscode/.dotnet/tools"
