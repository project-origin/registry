#!/bin/bash

# This script is a test of the registry and electricy verifier using the example client
# This script does the following
# - creates a kind cluster
# - generates issuing keys
# - creates two registries
# - issues consumption and production certificates
# - slices production certificate
# - claims certificate

# Ensures script fails if something goes wrong.
set -eo pipefail

# define variables
temp_folder=$(mktemp -d)
electricity_values_filename=${temp_folder}/electricity_values.yaml
registry_values_filename=${temp_folder}/registry_values.yaml
kind_filename=${temp_folder}/kind.yaml
example_area=Narnia

registry_a_name=test-a
registry_a_port=8080
registry_a_nodeport=32080
registry_a_namespace=ns-a

registry_b_name=test-b
registry_b_port=8081
registry_b_nodeport=32081
registry_b_namespace=ns-b

# define cleanup function
cleanup() {
    rm -fr $temp_folderx
    kind delete cluster --name ${cluster_name} >/dev/null 2>&1
}

# define debug function
debug() {
    echo -e "\nDebugging information:"
    echo -e "\nHelm list:"
    helm list --all-namespaces

    echo -e "\nHelm status Registry A:"
    helm status $registry_a_name --namespace ${ns-a} --show-desc --show-resources

    echo -e "\nHelm status Registry B:"
    helm status $registry_b_name --namespace ${ns-b} --show-desc --show-resources
}

# trap cleanup function on script exit
# trap 'cleanup' 0
# trap 'debug; cleanup' ERR
trap 'debug' ERR

# build docker image
docker build -f src/ProjectOrigin.Registry.Server/Dockerfile -t ghcr.io/project-origin/registry-server:test src/

# create kind configuration
cat << EOF > "$kind_filename"
kind: Cluster
apiVersion: kind.x-k8s.io/v1alpha4
nodes:
- role: control-plane
  extraPortMappings:
  - containerPort: $registry_a_nodeport
    hostPort: $registry_a_port
  - containerPort: $registry_b_nodeport
    hostPort: $registry_b_port
EOF

# recreate clean cluster
kind delete cluster -n helm-test
kind create cluster -n helm-test --config "$kind_filename"

# load docker image into cluster
kind load -n helm-test docker-image ghcr.io/project-origin/registry-server:test

# install postgresql in each namespace
helm install postgresql oci://registry-1.docker.io/bitnamicharts/postgresql --version 15.5.23 --namespace ${registry_a_namespace} --create-namespace
helm install rabbitmq oci://registry-1.docker.io/bitnamicharts/rabbitmq --version 14.6.6 --namespace ${registry_a_namespace}

helm install postgresql oci://registry-1.docker.io/bitnamicharts/postgresql --version 15.5.23 --namespace ${registry_b_namespace} --create-namespace
helm install rabbitmq oci://registry-1.docker.io/bitnamicharts/rabbitmq --version 14.6.6 --namespace ${registry_b_namespace}

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
      ${registry_b_name}:
        url: http://${registry_b_name}-postfix-service.${registry_b_namespace}:5000
    areas:
      $example_area:
        issuerKeys:
          - publicKey: $PublicKeyBase64
EOF

# install electricity verifier in default namespace
helm install electricity project-origin-verifier-electricity --repo https://project-origin.github.io/helm-registry --version 2.0.0-rc.3 -f "${electricity_values_filename}" --wait

# generate values for electricity verifier
cat << EOF > "${registry_values_filename}"
image:
  tag: test
service:
  type: NodePort
verifiers:
  - type: project_origin.electricity.v1
    url: http://verifier-electricity.default.svc.cluster.local:5000
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

# install two registries
echo "Installing registries"
helm install ${registry_a_name} -n ${registry_a_namespace} charts/project-origin-registry --set service.nodePort=$registry_a_nodeport -f "${registry_values_filename}"
helm install ${registry_b_name}-postfix -n ${registry_b_namespace} charts/project-origin-registry --set registryName=$registry_b_name,service.nodePort=$registry_b_nodeport -f "${registry_values_filename}"

# wait for all pods to be ready
kubectl wait --for=condition=available --timeout=300s deployment/test-a-deployment-0 deployment/test-a-deployment-1 deployment/test-a-deployment-2 -n ns-a
echo "Registry A installed"
kubectl wait --for=condition=available --timeout=300s deployment/test-b-postfix-deployment-0 deployment/test-b-postfix-deployment-1 deployment/test-b-postfix-deployment-2 -n ns-b
echo "Registry B installed"

# wait for cluster to be ready
sleep 15

# run tests
dotnet test src/ProjectOrigin.Registry.ChartTests \
  -e "AREA=$example_area" \
  -e "ISSUER_KEY=$PrivateKeyBase64" \
  -e "PROD_REGISTRY_NAME=$registry_a_name" \
  -e "PROD_REGISTRY_ADDRESS=http://localhost:$registry_a_port" \
  -e "CONS_REGISTRY_NAME=$registry_b_name" \
  -e "CONS_REGISTRY_ADDRESS=http://localhost:$registry_b_port" \
  -e "CONS_REGISTRY_BLOCKS=3"

echo "Test completed"
