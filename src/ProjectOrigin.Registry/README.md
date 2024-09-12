# Layer 4 - Privacy

![C4 component diagram of layer 4](/doc/layer4_privacy/component_diagra.drawio.svg)

## Requests

### IssueProduction
Role: IssuingBody
RequestBody: {m, r, from, to, gridArea, productionType..., publicM: bool }

### IssueConsumption
Role: IssuingBody
RequestBody: {m, r, from, to, gridArea, }

### TransferOwnership
Role: Owner
RequestBody: {m, r_transfer, r_remainer}


```protobuf
message IssueProduction {
  required int64 m = 1; // quantity to transfer
  required bytes r_transfer = 2; // r to hide transfer quantity behind
  required bytes r_remainer = 3; // r to hide remainder behind
}
```


### AllocateProductionClaim
Role: Owner
Description: Allocate part of a production certificate for a claim flow.
RequestBody: {m, r_claim, r_remainer, consumptionEventId, productionEventId, ref1, ref2}

### ClaimToConsumption
Role: Owner
Description: Initiate claim to a consumption certificate as part of claim flow.
RequestBody: {m, r_claim, r_remainer, consumptionEventId, productionEventId, ref1, ref2}

### ProductionClaimCommit
Role: Registry
RequestBody: {ref1, ref2}

### ConsumptionClaimCommit
Role: Registry
RequestBody: {ref2, ref3}
