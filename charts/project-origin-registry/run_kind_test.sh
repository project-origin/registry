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

# cleanup - delete temp_folder and cluster
trap 'rm -fr $temp_folder; kind delete cluster -n helm-test  >/dev/null 2>&1' 0

# define variables
temp_folder=$(mktemp -d)
override_values_filename=${temp_folder}/values_override.sh
kind_filename=${temp_folder}/kind.yaml
example_area=Narnia
registry_a_name=test-a
registry_a_port=8080
registry_a_nodeport=32080
registry_b_name=test-b
registry_b_port=8081
registry_b_nodeport=32081

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

# generate keys
PrivateKey=$(openssl genpkey -algorithm ED25519)
PrivateKeyBase64=$(echo "$PrivateKey" | base64 -w 0)
PublicKeyBase64=$(echo "$PrivateKey" | openssl pkey -pubout | base64 -w 0)

# generate values
cat << EOF > "${override_values_filename}"
verifiers:
  - name: electricity-v1
    type: ProjectOrigin.Electricity.v1
    image:
      repository: ghcr.io/project-origin/electricity-server
      tag: 0.2.0-rc.13
    issuers:
      - area: $example_area
        publicKey: $PublicKeyBase64
    registries:
      - name: ${registry_a_name}
        address: http://registry-${registry_a_name}:80
      - name: ${registry_b_name}
        address: http://registry-${registry_b_name}:80
EOF

# install two registries
helm install ${registry_a_name} charts/project-origin-registry --set service.nodePort=$registry_a_nodeport,service.type=NodePort -f "${override_values_filename}" --wait >/dev/null 2>&1
echo "Registry A installed"
helm install ${registry_b_name} charts/project-origin-registry --set service.nodePort=$registry_b_nodeport,service.type=NodePort -f "${override_values_filename}" --wait >/dev/null 2>&1
echo "Registry B installed"

# wait for cluster to be ready
sleep 15

# run test
dotnet run --project src/ProjectOrigin.Electricity.Example WithoutWalletFlow $example_area $PrivateKeyBase64 ${registry_a_name} http://localhost:$registry_a_port ${registry_b_name} http://localhost:$registry_b_port
echo "Test completed"
