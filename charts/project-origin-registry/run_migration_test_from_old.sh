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
override_values_filename=${temp_folder}/values_override.yaml
registry_values_filename=${temp_folder}/registry_values.yaml
kind_filename=${temp_folder}/kind.yaml
example_area=Narnia
registry_name=test-a
registry_port=8090
registry_port_1=8091
registry_nodeport=32080
registry_nodeport_1=32081
registry_namespace=ns-a

# define cleanup function
cleanup() {
    rm -fr $temp_folderx
    # kind delete cluster --name ${cluster_name} >/dev/null 2>&1
}

# define debug function
debug() {
    echo -e "\nDebugging information:"
    echo -e "\nHelm list:"
    helm list --all-namespaces --kube-context kind-${cluster_name}

    echo -e "\nHelm status Registry A:"
    helm status $registry_name --namespace ${registry_namespace} --show-desc --show-resources --kube-context kind-${cluster_name}
}

# trap cleanup function on script exit
trap 'cleanup' 0
trap 'debug; cleanup' ERR

# create kind configuration
cat << EOF > "$kind_filename"
kind: Cluster
apiVersion: kind.x-k8s.io/v1alpha4
nodes:
- role: control-plane
  extraPortMappings:
  - containerPort: $registry_nodeport
    hostPort: $registry_port
  - containerPort: $registry_nodeport_1
    hostPort: $registry_port_1
EOF

# recreate clean cluster
kind delete cluster -n ${cluster_name}
kind create cluster -n ${cluster_name} --config "$kind_filename"

# install rabbitmq-operator
kubectl apply -f "https://github.com/rabbitmq/cluster-operator/releases/download/v2.7.0/cluster-operator.yml"

# install cnpg-operator
helm install cnpg-operator cloudnative-pg --repo https://cloudnative-pg.io/charts --version 0.18.0 --namespace cnpg --create-namespace --wait

# generate keys
PrivateKey=$(openssl genpkey -algorithm ED25519)
PrivateKeyBase64=$(echo "$PrivateKey" | base64 -w 0)
PublicKeyBase64=$(echo "$PrivateKey" | openssl pkey -pubout | base64 -w 0)

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
      - name: ${registry_name}
        address: http://registry-${registry_name}.${registry_namespace}:5000
persistance:
  cloudNativePG:
    enabled: true
service:
  type: NodePort
  nodePort: $registry_nodeport
EOF

# install most recent registry-chart version
helm install ${registry_name} project-origin-registry --version 1.3.1 -f "${override_values_filename}" -n ${registry_namespace} --repo https://project-origin.github.io/helm-registry --create-namespace --wait
echo "Registry installed"

# wait for cluster to be ready
sleep 15

# run tests
dotnet test test/ProjectOrigin.Registry.ChartTests \
  -e "AREA=$example_area" \
  -e "ISSUER_KEY=$PrivateKeyBase64" \
  -e "PROD_REGISTRY_NAME=$registry_name" \
  -e "PROD_REGISTRY_ADDRESS=http://localhost:$registry_port" \
  -e "CONS_REGISTRY_NAME=$registry_name" \
  -e "CONS_REGISTRY_ADDRESS=http://localhost:$registry_port" \
  -e "CONS_REGISTRY_BLOCKS=7"

# ---------------------------------------------------
# ---------------- Remove from HELM -----------------

# kubectl annotate rabbitmqclusters.rabbitmq.com ${registry_name}-rabbitmq --overwrite -n ${registry_namespace} helm.sh/resource-policy=keep
# kubectl annotate rabbitmqclusters.rabbitmq.com ${registry_name}-rabbitmq --overwrite -n ${registry_namespace} meta.helm.sh/release-name-
# kubectl annotate rabbitmqclusters.rabbitmq.com ${registry_name}-rabbitmq --overwrite -n ${registry_namespace} meta.helm.sh/release-namespace-
# kubectl label rabbitmqclusters.rabbitmq.com ${registry_name}-rabbitmq --overwrite -n ${registry_namespace} app.kubernetes.io/managed-by-

# kubectl annotate clusters.postgresql.cnpg.io cnpg-registry-db --overwrite -n ${registry_namespace} helm.sh/resource-policy=keep
# kubectl annotate clusters.postgresql.cnpg.io cnpg-registry-db --overwrite -n ${registry_namespace} meta.helm.sh/release-name-
# kubectl annotate clusters.postgresql.cnpg.io cnpg-registry-db --overwrite -n ${registry_namespace} meta.helm.sh/release-namespace-
# kubectl label clusters.postgresql.cnpg.io cnpg-registry-db --overwrite -n ${registry_namespace} app.kubernetes.io/managed-by-
# ---------------------------------------------------
# ---------------------------------------------------
# generate values for electricity verifier
cat << EOF > "${electricity_values_filename}"
networkConfig:
  yaml: |-
    registries:
      ${registry_name}:
        url: http://${registry_name}-service.${registry_namespace}:5000
    areas:
      $example_area:
        issuerKeys:
          - publicKey: $PublicKeyBase64
EOF

# generate values for electricity verifier
cat << EOF > "${registry_values_filename}"
service:
  type: NodePort
  nodePort: ${registry_nodeport_1}
verifiers:
  - type: project_origin.electricity.v1
    url: http://verifier-electricity.default.svc.cluster.local:5000
transactionProcessor:
  replicas: 1
blockFinalizer:
  interval: 00:00:10
postgresql:
  host: cnpg-registry-db-rw
  database: registry-database
  username:
    secretRef:
      name: cnpg-registry-db-app
      key: username
  password:
    secretRef:
      name: cnpg-registry-db-app
      key: password
rabbitmq:
  host:
    secretRef:
      name: test-a-rabbitmq-default-user
      key: host
  username:
    secretRef:
      name: test-a-rabbitmq-default-user
      key: username
  password:
    secretRef:
      name: test-a-rabbitmq-default-user
      key: password
redis:
  replica:
    replicaCount: 1
EOF

# Updates registry to the one in tree and runs tests
echo "Updating registry"
helm upgrade ${registry_name} -n ${registry_namespace} project-origin-registry --version 2.0.0-rc.1 -f "${registry_values_filename}" --repo https://project-origin.github.io/helm-registry --kube-context kind-${cluster_name}
helm install electricity project-origin-verifier-electricity --repo https://project-origin.github.io/helm-registry --version 2.0.0-rc.6 -f "${electricity_values_filename}" --wait --kube-context kind-${cluster_name}
kubectl wait --for=condition=available --timeout=300s deployment/${registry_name}-deployment-0 -n ${registry_namespace} --context kind-${cluster_name}

echo "Registry updated"

dotnet test test/ProjectOrigin.Registry.ChartTests \
  -e "AREA=$example_area" \
  -e "ISSUER_KEY=$PrivateKeyBase64" \
  -e "PROD_REGISTRY_NAME=$registry_name" \
  -e "PROD_REGISTRY_ADDRESS=http://localhost:$registry_port_1" \
  -e "CONS_REGISTRY_NAME=$registry_name" \
  -e "CONS_REGISTRY_ADDRESS=http://localhost:$registry_port_1" \
  -e "CONS_REGISTRY_BLOCKS=14"
