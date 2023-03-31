FROM mcr.microsoft.com/devcontainers/dotnet:7.0

# DocFX v2.64.0
USER vscode
RUN dotnet tool install --global docfx --version 2.64.0 &&\
    echo "export PATH=/home/vscode/.dotnet/tools:${PATH}" >> /home/vscode/.bashrc
