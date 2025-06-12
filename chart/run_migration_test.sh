#!/bin/bash

# This script is a test that the registry chart can be upgraded from the current most recent version to the one in tree.
# This script does the following
# - creates a kind cluster
# - generates issuing keys
# - installs a registry
# - runs tests
# - updates the registry
# - runs tests again
# - cleans up

# Ensures script fails if something goes wrong.
set -eo pipefail

# define variables
cluster_name=migration-test
temp_folder=$(mktemp -d)
electricity_values_filename=${temp_folder}/electricity_values.yaml
registry_values_filename=${temp_folder}/registry_values.yaml
kind_filename=${temp_folder}/kind.yaml
example_area=Narnia
registry_a_name=test-a
registry_a_port=8090
registry_a_nodeport=32080
registry_a_namespace=ns-a

# define cleanup function
cleanup() {
    rm -fr $temp_folderx
    #kind delete cluster --name ${cluster_name} >/dev/null 2>&1
}

# define debug function
debug() {
    echo -e "\nDebugging information:"
    echo -e "\nHelm list:"
    helm list --all-namespaces --kube-context kind-${cluster_name}

    echo -e "\nHelm status Registry A:"
    helm status $registry_a_name --namespace ${registry_a_namespace} --show-desc --show-resources --kube-context kind-${cluster_name}
    kubectl logs -l app=${registry_a_name}-registry --namespace ${registry_a_namespace}  --all-containers=true
}

# trap cleanup function on script exit
trap 'cleanup' 0
trap 'debug; cleanup' ERR

# build docker image
make build-container

# create kind configuration
cat << EOF > "$kind_filename"
kind: Cluster
apiVersion: kind.x-k8s.io/v1alpha4
nodes:
- role: control-plane
  extraPortMappings:
  - containerPort: $registry_a_nodeport
    hostPort: $registry_a_port
EOF

# recreate clean cluster
kind delete cluster -n ${cluster_name}
kind create cluster -n ${cluster_name} --config "$kind_filename"

# load docker image into cluster
kind load -n ${cluster_name} docker-image ghcr.io/project-origin/registry-server:test

# install postgresql in each namespace
helm install postgresql oci://registry-1.docker.io/bitnamicharts/postgresql --version 15.5.23 --namespace ${registry_a_namespace} --create-namespace --kube-context kind-${cluster_name}
helm install rabbitmq oci://registry-1.docker.io/bitnamicharts/rabbitmq --version 14.6.6 --namespace ${registry_a_namespace} --kube-context kind-${cluster_name}

# generate keys
PrivateKey=$(openssl genpkey -algorithm ED25519)
PrivateKeyBase64=$(echo "$PrivateKey" | base64 -w 0)
PublicKeyBase64=$(echo "$PrivateKey" | openssl pkey -pubout | base64 -w 0)

# generate values for electricity verifier
cat << EOF > "${electricity_values_filename}"
networkConfig:
  yaml: |-
    registries:
      ${registry_a_name}:
        url: http://${registry_a_name}-service.${registry_a_namespace}:5000
    areas:
      $example_area:
        issuerKeys:
          - publicKey: $PublicKeyBase64
EOF

# install electricity verifier in default namespace
helm install electricity project-origin-verifier-electricity --repo https://project-origin.github.io/helm-registry --version 4.0.0 -f "${electricity_values_filename}" --wait --kube-context kind-${cluster_name}

# generate values for electricity verifier
cat << EOF > "${registry_values_filename}"
service:
  type: NodePort
  nodePort: ${registry_a_nodeport}
verifiers:
  - type: project_origin.electricity.v1
    url: http://electricity.default.svc.cluster.local:5000
transactionProcessor:
  replicas: 1
returnComittedForFinalized: false
blockFinalizer:
  interval: 00:00:15
postgresql:
  host: postgresql
  database: postgres
  username: postgres
  password:
    secretRef:
      name: postgresql
      key: postgres-password
rabbitmq:
  host: rabbitmq
  username: user
  password:
    secretRef:
      name: rabbitmq
      key: rabbitmq-password
redis:
  replica:
    replicaCount: 1
EOF

# install registry from
echo "Installing latest released registry"
helm install ${registry_a_name} -n ${registry_a_namespace} project-origin-registry --version 3.0.1 -f "${registry_values_filename}" --repo https://project-origin.github.io/helm-registry --kube-context kind-${cluster_name}
kubectl wait --for=condition=available --timeout=300s deployment/${registry_a_name}-deployment-0 -n ${registry_a_namespace} --context kind-${cluster_name}
echo "Registry A installed"

# run tests
dotnet test test/ProjectOrigin.Registry.ChartTests \
  -e "AREA=$example_area" \
  -e "ISSUER_KEY=$PrivateKeyBase64" \
  -e "PROD_REGISTRY_NAME=$registry_a_name" \
  -e "PROD_REGISTRY_ADDRESS=http://localhost:$registry_a_port" \
  -e "CONS_REGISTRY_NAME=$registry_a_name" \
  -e "CONS_REGISTRY_ADDRESS=http://localhost:$registry_a_port" \
  -e "CONS_REGISTRY_BLOCKS=7"

# Updates registry to the one in tree and runs tests
echo "Updating registry"
helm upgrade ${registry_a_name} -n ${registry_a_namespace} chart --set image.tag=test -f "${registry_values_filename}" --kube-context kind-${cluster_name}
kubectl wait --for=condition=available --timeout=300s deployment/${registry_a_name}-deployment-0 -n ${registry_a_namespace} --context kind-${cluster_name}
echo "Registry updated"

dotnet test test/ProjectOrigin.Registry.ChartTests \
  -e "AREA=$example_area" \
  -e "ISSUER_KEY=$PrivateKeyBase64" \
  -e "PROD_REGISTRY_NAME=$registry_a_name" \
  -e "PROD_REGISTRY_ADDRESS=http://localhost:$registry_a_port" \
  -e "CONS_REGISTRY_NAME=$registry_a_name" \
  -e "CONS_REGISTRY_ADDRESS=http://localhost:$registry_a_port" \
  -e "CONS_REGISTRY_BLOCKS=14"
