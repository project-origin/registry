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

# cleanup - delete temp_folder and cluster
trap 'rm -fr $temp_folder; kind delete cluster -n helm-test  >/dev/null 2>&1' 0

# define variables
temp_folder=$(mktemp -d)
override_values_filename=${temp_folder}/values_override.sh
kind_filename=${temp_folder}/kind.yaml
cnpg_filename=${temp_folder}/cnpg.yaml
example_area=Narnia
registry_a_name=test-a
registry_a_port=8080
registry_a_nodeport=32080
registry_a_namespace=ns-a

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
EOF

# recreate clean cluster
kind delete cluster -n helm-test
kind create cluster -n helm-test --config "$kind_filename"

# install rabbitmq-operator
kubectl apply -f "https://github.com/rabbitmq/cluster-operator/releases/download/v2.7.0/cluster-operator.yml"

# load docker image into cluster
kind load -n helm-test docker-image ghcr.io/project-origin/registry-server:test

# install cnpg-operator
helm install cnpg-operator cloudnative-pg --repo https://cloudnative-pg.io/charts --version 0.18.0 --namespace cnpg --create-namespace --wait

# TODO: Uncomment after merging
# # setup cnpg cluster
# cat << EOF > "$cnpg_filename"
# apiVersion: v1
# kind: Namespace
# metadata:
#   name: $registry_a_namespace
# ---
# apiVersion: rabbitmq.com/v1beta1
# kind: RabbitmqCluster
# metadata:
#   name: ${registry_a_name}-rabbitmq
#   namespace: $registry_a_namespace
# ---
# apiVersion: postgresql.cnpg.io/v1
# kind: Cluster
# metadata:
#   name: cnpg-registry-db
#   namespace: $registry_a_namespace
# spec:
#   instances: 3
#   storage:
#     size: 10Gi
#   bootstrap:
#     initdb:
#       database: registry-database
#       owner: app
#   monitoring:
#     enablePodMonitor: true
# EOF

kubectl apply -f "$cnpg_filename"

# generate keys
PrivateKey=$(openssl genpkey -algorithm ED25519)
PrivateKeyBase64=$(echo "$PrivateKey" | base64 -w 0)
PublicKeyBase64=$(echo "$PrivateKey" | openssl pkey -pubout | base64 -w 0)

# generate values
cat << EOF > "${override_values_filename}"
verifiers:
  - name: electricity-v1
    type: project_origin.electricity.v1
    image:
      repository: ghcr.io/project-origin/electricity-server
      tag: 0.5.0
    issuers:
      - area: $example_area
        publicKey: $PublicKeyBase64
    registries:
      - name: ${registry_a_name}
        address: http://registry-${registry_a_name}.${registry_a_namespace}:5000
EOF

# install most recent registry-chart version
echo "Install registry"
helm install ${registry_a_name} project-origin-registry --set persistance.cloudNativePG.enabled=true,image.tag=test,service.nodePort=$registry_a_nodeport,service.type=NodePort -f "${override_values_filename}" -n ${registry_a_namespace} --repo https://project-origin.github.io/helm-registry --create-namespace --wait
echo "Registry installed"

# wait for cluster to be ready
sleep 15

# run tests
dotnet test src/ProjectOrigin.Registry.ChartTests \
  -e "AREA=$example_area" \
  -e "ISSUER_KEY=$PrivateKeyBase64" \
  -e "PROD_REGISTRY_NAME=$registry_a_name" \
  -e "PROD_REGISTRY_ADDRESS=http://localhost:$registry_a_port" \
  -e "CONS_REGISTRY_NAME=$registry_a_name" \
  -e "CONS_REGISTRY_ADDRESS=http://localhost:$registry_a_port" \
  -e "CONS_REGISTRY_BLOCKS=7"

# Updates registry to the one in tree and runs tests
echo "Updating registry"
helm upgrade ${registry_a_name} -n ${registry_a_namespace} charts/project-origin-registry --set persistance.cloudNativePG.enabled=true,image.tag=test,service.nodePort=$registry_a_nodeport,service.type=NodePort -f "${override_values_filename}"  --create-namespace --wait
echo "Registry updated"

dotnet test src/ProjectOrigin.Registry.ChartTests \
  -e "AREA=$example_area" \
  -e "ISSUER_KEY=$PrivateKeyBase64" \
  -e "PROD_REGISTRY_NAME=$registry_a_name" \
  -e "PROD_REGISTRY_ADDRESS=http://localhost:$registry_a_port" \
  -e "CONS_REGISTRY_NAME=$registry_a_name" \
  -e "CONS_REGISTRY_ADDRESS=http://localhost:$registry_a_port" \
  -e "CONS_REGISTRY_BLOCKS=14"
