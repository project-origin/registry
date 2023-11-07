# Quickstart

There will soon be a guide how to run the registry server.

It will be written when the helm chart is created.


## Generating a issuer key

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

### Deriving public key

To derive the public key to be shared with the registry verifiers one
can use openssl, here the key is written to a file named
narnia.pub

```shell
openssl pkey -in narnia.pem -pubout > narnia.pub
```

### Add it values.yaml file

To add the narnia.pub to the values file,
one must encode the file as base64,
this can again be done using the shell

```shell
cat narnia.pub | base64 -w 0
```

> note: the `-w 0` is to disable word-wrap of the output depending on the platform
