# Issue Command

> [!NOTE]
> The issue command is only valid for **Issuing bodies** for a given area.

It enables the issuing body to issue a new Granular Certificate.

When issued, **1** initial slice is created. The **quantity** and **publickey** parameters are used to create the initial slice.

## How to

Look at the CommandBuilder for information on how to perform the two issue commands:

- [Issued Consumption GC](xref:ProjectOrigin.Electricity.Client.ElectricityCommandBuilder.IssueConsumptionCertificate(ProjectOrigin.Electricity.Client.Models.FederatedCertifcateId,ProjectOrigin.Electricity.Client.Models.DateInterval,System.String,ProjectOrigin.Electricity.Client.Models.ShieldedValue,ProjectOrigin.Electricity.Client.Models.ShieldedValue,PublicKey,Key))
- [Issued Production GC](xref:ProjectOrigin.Electricity.Client.ElectricityCommandBuilder.IssueProductionCertificate(ProjectOrigin.Electricity.Client.Models.FederatedCertifcateId,ProjectOrigin.Electricity.Client.Models.DateInterval,System.String,System.String,System.String,ProjectOrigin.Electricity.Client.Models.ShieldedValue,ProjectOrigin.Electricity.Client.Models.ShieldedValue,PublicKey,Key))
