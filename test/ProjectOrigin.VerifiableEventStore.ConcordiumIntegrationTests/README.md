# Integration test with Concordium

To be able to run the integration test with Concordium,
a Concordium node and github self-hosted runner is required.

This can be achieved using the included [docker-compose](../ProjectOrigin.VerifiableEventStore.Tests/docker-compose.yaml).

## Running a node
1. Make a .env file containing the following environment variables:

YOUR_GITHUB_ORGANIZATION_PAT should be a Personal Access Token with the accesss to create GitHub runners on an organization level.

```sh
GITHUB_RUNNER_PAT=${YOUR_GITHUB_ORGANIZATION_PAT}
CONCORDIUM_HOST_DIRECTORY=/var/concordium/data
```

2. Next get the docker-compose file and run it. ***Note: it takes quite a while before the node has processed all blocks and are ready. (hours)***

    The long time for the node to be ready is why this is done in this matter instead of in a GitHub workflow, since it wouldn't be feasible.

```sh
#wget -qO- https://raw.githubusercontent.com/project-origin/registry/main/src/ProjectOrigin.VerifiableEventStore.Tests/docker-compose.yaml | docker-compose -f - up -d
wget -qO- https://raw.githubusercontent.com/project-origin/registry/eventstore/integration-tests/src/ProjectOrigin.VerifiableEventStore.Tests/docker-compose.yaml | docker-compose -f - up -d --build
```

3. One can use the following command, to get what block it has gotten to from the node.

```sh
concordium-client --grpc-port 10001 block show
```

## Creating an identity

1. Create an Identity and Account with Concordium.

    To create these, follow the [Concordium documentation](https://developer.concordium.software/en/mainnet/net/guides/company-identities.html)
for the testnet. More info can be found [here](https://github.com/Concordium/concordium-base/blob/main/rust-bins/docs/user-cli.md#generate-a-version-0-request-for-the-version-0-identity-object
).

```sh
user_cli generate-request --cryptographic-parameters cryptographic-parameters-testnet.json \
                          --ars ars-testnet.json \
                          --ip-info ip-info-testnet.json \
                          --initial-keys-out initial-keys.json \ # keys of the initial account together with its address.
                          --id-use-data-out id-use-data.json \ # data that enables use of the identity object
                          --request-out request.json # request to send to the identity provider
```

2. Wait for Concordium to respond with the **id-object.json**.

3. Create the account as described in the [Concordium doc](https://github.com/Concordium/concordium-base/blob/main/rust-bins/docs/user-cli.md#create-accounts-from-a-version-0-identity-object)

```sh
user_cli create-credential --id-use-data id-use-data.json \
                           --id-object id-object.json \
                           --keys-out account-keys.json \
                           --credential-out credential.json
```

4. Upload credential to concordium

```sh
concordium-client transaction deploy-credential credential.json
```

## Configure GitHub environment

1. Create a Environment on GitHub named testnet

2. Add 2 secrets:

    1. AccountAddress - should contain the address to pay for the tests.
    2. AccountKey - should contain the hex key for the account.

3. Run tests
