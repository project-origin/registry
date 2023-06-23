FROM mcr.microsoft.com/devcontainers/dotnet:7.0

# DocFX v2.67.3
USER vscode
RUN dotnet tool install --global docfx --version 2.67.3
ENV PATH "$PATH:/home/vscode/.dotnet/tools"
