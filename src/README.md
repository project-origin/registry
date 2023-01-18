# Source content

This file describes the content of the individual source folders.

## ProjectOrigin.Electricity

Implements Verifiers and Orchestrators used by the Command and Step processors.
Uses the defined protobuf schema to serialize Electricity commands and events defined.

## ProjectOrigin.Electricity.Client

Uses gRPC based on the .proto files that define the command service and messages to call the CommandProcessor
Uses the defined protobuf schema to serialize Electricity commands and events defined.

## ProjectOrigin.Electricity.IntegrationTests

Contains integrations tests running a local TestServer that contains the full stack (with Mock blockchain connector)
using the client-library to do full tests.

## ProjectOrigin.Electricity.Server

Holds the configuration to start a registry server.

## ProjectOrigin.Electricity.Tests

Contains unit-tests for the ProjectOrigin.Electricity library.

## ProjectOrigin.PedersenCommitment

Contains a C# implementation of a PedersenCommitment based on a finite set.

## ProjectOrigin.PedersenCommitment.Tests

Contains unit-tests for the ProjectOrigin.PedersenCommitment library.

## ProjectOrigin.Register.CommandProcessor

Receives incoming commands from the Client, uses IOrchestrators to orchestrate the command steps, before returning the result to the client.
Uses gRPC based on the .proto files that define the command service and messages.

## ProjectOrigin.Register.StepProcessor

Takes individual CommandSteps, and verifies them against IVerifiers before storing them in the event-store.

## ProjectOrigin.Register.StepProcessor.Tests

Contains unit-tests for the ProjectOrigin.Register.StepProcessor library.

## ProjectOrigin.Register.Utils

Contains utils that are shared between the StepProcessor and CommandProcessor

## ProjectOrigin.VerifiableEventStore

Stores Verifiable events in a series of Merkle trees, ensuring they are tamper proof.

## ProjectOrigin.VerifiableEventStore.ConcordiumIntegrationTests

Contains integrations tests of the Concordium blockchain connector, requires access to running concordium node and environment variables.

## ProjectOrigin.VerifiableEventStore.Tests

Contains unit-tests for the ProjectOrigin.VerifiableEventStore library.

## Protos

Contains the .proto files defining the different messages and services in the registry.

## rust-ffi

Contains the Rust library used for eliptic curves, bulletproofs and commitments.
To built this you will require the rust toolchain along with Cargo.

