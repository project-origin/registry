FROM mcr.microsoft.com/devcontainers/dotnet:7.0

# DocFX v2.70.0
USER vscode
RUN dotnet tool install --global docfx --version 2.70.0
ENV PATH "$PATH:/home/vscode/.dotnet/tools"
