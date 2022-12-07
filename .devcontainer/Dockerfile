FROM mcr.microsoft.com/devcontainers/dotnet:7.0

# Install Mono and DocFX v2.59.4
RUN sudo apt install apt-transport-https dirmngr gnupg ca-certificates &&\
    sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF &&\
    echo "deb https://download.mono-project.com/repo/debian stable-buster main" | sudo tee /etc/apt/sources.list.d/mono-official-stable.list &&\
    sudo apt update &&\
    sudo apt install mono-devel -y &&\
    sudo wget -O /tmp/docfx.zip https://github.com/dotnet/docfx/releases/download/v2.59.4/docfx.zip &&\
    sudo unzip -o /tmp/docfx.zip -d /opt/docfx &&\
    echo '#!/bin/bash\nmono /opt/docfx/docfx.exe $@' > /usr/bin/docfx && chmod +x /usr/bin/docfx
