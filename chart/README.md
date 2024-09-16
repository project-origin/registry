# Project Origin - Registry

This Helm chart is used to deploy a registry for the Project Origin

## WARNING ⚠️

From version 2.0 there is a breaking changes from 1.x, the `cloudnative pg` and `rabbitmq operator` has been removed from the chart.
Please first migrate to `1.3.1` to ensure helm does not delete the database.

From 2.0 the database, rabbitmq, verifier and concordium must be installed separately.

## Requirements

The registry requires the following other components to have been installed:

- A ProjectOrigin verifier
- PostgreSQL database
- RabbitMQ message broker with rabbitmq_management plugin enabled

### Verifier

To install a verifier one can find the ETT electricity chart at [here](https://artifacthub.io/packages/helm/project-origin/project-origin-verifier-electricity), and the guide on how to install it in its readme.

When installed, the registry must be configured using the following:

```yaml
verifiers:
  - type: project_origin.electricity.v1
    url: http://${SERVICENAME}.${NAMESPACE}.svc.cluster.local:5000
```

### PostgreSQL

The registry requires a PostgreSQL database to store its data.

Below is an example of how to install a PostgreSQL database using the bitnami helm chart:

```shell
helm install my-postgres oci://registry-1.docker.io/bitnamicharts/postgresql --version 15.5.23
```

The registry must be configured to use the database using the following, this assumes same namespace as the registry:
```yaml
postgresql:
  host: my-postgres
  database: postgres
  username: postgres
  password:
    secretRef:
      name: my-postgres
      key: postgres-password
```

### RabbitMQ

The registry requires a RabbitMQ message broker to queue and process transactions.

Below is an example of how to install a RabbitMQ message broker using the bitnami helm chart:

```shell
helm install my-rabbitmq oci://registry-1.docker.io/bitnamicharts/rabbitmq --version 14.6.6
```

The registry must be configured to use the RabbitMQ using the following, this assumes same namespace as the registry:
```yaml
rabbitmq:
  host: my-rabbitmq
  username: user
  password:
    secretRef:
      name: my-rabbitmq
      key: rabbitmq-password
```

## Installing the Chart

To install the chart with the release name `my-example-registry` and configured using `values.yaml` one can use the following command:

```shell
helm install -f values.yaml my-example-registry project-origin-registry --version 0.2.0-rc.1 --repo https://project-origin.github.io/helm-registry .
```

## Ingress

The chart does not include ingress, but must be configured if to expose it externally.

## Verify the setup

To verify the setup one can use the Electricy Example available in the
[project-origin/registry](https://github.com/project-origin/registry) repository
and using the key generated when installing the verifier.

To run the example one can use the following command:

```shell
EXAMPLE_AREA="Narnia"
PRIVATE_KEY_BASE64=$(cat narnia.pem | base64 -w 0)
REGISTRY_NAME="my-example-registry"
REGISTRY_URL= # the url of the registry, e.g. http://my-example-registry:5000
dotnet run --project src/ProjectOrigin.Registry.ChartTests WithoutWalletFlow $example_area $PrivateKeyBase64 $REGISTRY_NAME $REGISTRY_URL
```
