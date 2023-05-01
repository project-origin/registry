# Base ubuntu image
FROM ubuntu:20.04

# Github runner version https://github.com/actions/runner/releases
ARG VERSION="2.303.0"
ARG PAT
ARG REPOSITORY="https://github.com/project-origin"
ARG NAME="concordium-workflow-runner"
ARG LABELS="concordium-testnet"

# Set env variables
ENV DEBIAN_FRONTEND=noninteractive

# Update and install packages
RUN apt-get update -y &&\
    apt-get upgrade -y &&\
    apt-get install -y --no-install-recommends curl ca-certificates unzip sudo git make

# Configure user and ownership
RUN useradd -m runner &&\
    usermod -aG sudo runner &&\
    echo '%sudo ALL=(ALL) NOPASSWD:ALL' >> /etc/sudoers

# Set workdir
WORKDIR /home/runner/actions-runner

# Mkdir for runner, download, unzip
RUN curl -o runner.tar.gz -L https://github.com/actions/runner/releases/download/v${VERSION}/actions-runner-linux-x64-${VERSION}.tar.gz &&\
    tar xzf runner.tar.gz &&\
    rm runner.tar.gz &&\
    ./bin/installdependencies.sh &&\
    chown -R runner /home/runner &&\
    chown -R runner /usr/share

# Set user as "runner"
USER runner

# Configure runner
RUN ./config.sh --unattended --url ${REPOSITORY} --pat ${PAT} --replace --name ${NAME} --labels ${LABELS}

ENTRYPOINT ./run.sh
