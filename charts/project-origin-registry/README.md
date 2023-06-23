# Project Origin Stack

This Helm chart enables one to install all the components required to run Project Origin
on a kubernetes cluster.

## Quickstart

This assumes general understanding of kubernetes and helm.

The minumum configuration required locally is the following values file:

```yaml
# defines the verifiers for the registry
verifiers:
  - name: electricity-v1
    type: ProjectOrigin.Electricity.v1
    image:
      repository: ghcr.io/project-origin/electricity-server
      tag: 0.2.0-rc.1
    issuers:
        # the name of the grid area, here Narnia is used
      - area: Narnia
        # the base64 encoded public key of the issuer
        publicKey: #BASE64_ENCODED_PUBLIC_KEY

    # if you want to use multiple registries, then ALL verifiers must know all registries External url
    # this is because the verifiers will use the external url to communicate with the registries
    registries:
      - name: my-example-registry
        address: http://my-example-registry:80
```

Once one have generated a key and added it to the values file,
one can install the chart using the following command:

```shell
helm install -f values.yaml my-example-registry project-origin-registry --version 0.2.0-rc.1 --repo https://project-origin.github.io/helm-registry .
```

### 1. Generating a issuer key

An issuer key is the public-private key-pair used by an issuing body
to issue certificates on the registries.

Issuer algorithm used is the ED25519 curve,
this is one of the most used curves for signing and is in broad use
and is tried and tested.

To generate a private key one can use openssl,
below we generate a key for narnia.

```shell
openssl genpkey -algorithm ED25519 -out narnia.pem
```

> NOTE: This is the private key which must be kept secure

#### Deriving public key

To derive the public key to be shared with the registry verifiers one
can use openssl, here the key is written to a file named
narnia.pub

```shell
openssl pkey -in narnia.pem -pubout > narnia.pub
```

#### Add it values.yaml file

To add the narnia.pub to the values file,
one must encode the file as base64,
this can again be done using the shell

```shell
cat narnia.pub | base64 -w 0
```

> note: the `-w 0` is to disable word-wrap of the output depending on the platform

### Ingress

The chart does not currently support ingress, but it is possible to
use ingress to expose the registry.

To do this one create an ingress resource which points to the service
using ones favorite ingress controller.

This will come in near future.

#### Verify the setup

To verify the setup one can use the Electricy Example available in the
[project-origin/registry](https://github.com/project-origin/registry) repository.

To run the example one can use the following command:

```shell
EXAMPLE_AREA="Narnia"
PRIVATE_KEY_BASE64=$(cat narnia.pem | base64 -w 0)
REGISTRY_NAME="my-example-registry"
REGISTRY_URL= # the url of the registry, e.g. http://my-example-registry:80
dotnet run --project src/ProjectOrigin.Electricity.Example WithoutWalletFlow $example_area $PrivateKeyBase64 $REGISTRY_NAME $REGISTRY_URL
```
