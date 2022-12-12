# Overview of Energy Track and Trace

## Certification of Energy
Why bother at all one might ask, the energy supplied in the socket is one thing and its origin is what is available at any given time ? The answer is that the energy supplied in the socket is not one thing, it is a mix of energy sources, some of which are renewable and some of which are not. The energy supplied in the socket is not what is available at any given time, it is a mix of energy sources, some of which are renewable and some of which are not. We cannot trace the individual electron, but with the extensive infrastructure that is rolled out using metering data - we can leverage the data to provide a certification of the energy supplied, not just at a yearly basis, but on a basis aligned with the spot market and the energy supplied. This is the basis of the Energy Track and Trace (ETT) project.

![](/resources/graphics/ETTvalue.drawio.png)

## How it has been done so far ?
As the penetration of shares of renewables has increased towards 30% in some countries, the need for a system to certify the energy supplied has become more and more important. The existing system is based on Guarantees of Origin (GO) which are issued yearly and are based on the energy produced in a year. This system has been in place for a long time and is well understood. 

### GO Guarantees of Origin
The existing system is based on Guarantees of Origin (GO) which are issued yearly and are based on the energy produced in a year. This system has been in place for a long time and is well understood. The system is based on the following principles:

1. The energy produced is certified by a third party issuer compliant with a central authority (AIB), and the certificate is issued to the producer
2. The certificate is then sold to the supplier
3. The supplier then sells the certificate to the consumer
4. The consumer can then use the certificate to claim/retire/cancel that the energy supplied is renewable

The existing GO system is based on the following principles:
- Yearly certification
- Volume based accounting
- Exchange of certificates between parties through a centralised system AIB-hub
- The certificate is a tradable asset
- No direct physical link between the certificate and the energy supplied are required

With the penetration of renewable energy increasing, at times more than 100% of certain grid areas are supplied by renewables, it brings a number of challenges to the existing system: 
- Makes it hard to balance the energy grid to 50Hz
- Creates bottlenecks in the existing grid 
- Requires loadbalancing that is less centralized 
- Makes it hard to validate the actual energy supplied to power-to-x applications and the origin of the losses in the process

Guarantees of Origin are based on a standard developed by the European Committee for Standardisation (CEN) and the European Committee for Electrotechnical Standardisation (CENELEC) and is called EN 16325. [EECS and the CEN standard EN16325](https://www.aib-net.org/eecs) AIB is the central authority for the GO system in Europe and is responsible for the certification of the issuers and the central hub for the exchange of certificates. [AIB](https://www.aib-net.org/) The regulation is based on the Renewable Energy Directive (RED) and the [REDII - regulation](https://energy.ec.europa.eu/topics/renewable-energy/renewable-energy-directive-targets-and-rules/renewable-energy-directive_en).

It is by no means an extensive and exhaustive analysis of the existing Guarantees of Origin system, but it is a good starting point for understanding the system. Tackling the challenges of the existing system is the basis for the Energy Track and Trace project and a strategy for tackling the challenges is described in the next section.

### Strategies that could be applied to the problem
Several strategies could be applied to the infrastructure to tackle the challenges of the existing system. 

#### Direct infrastructure
Simply adding a direct link between the energy supplied and the certificate would be a simple solution, but it would require a lot of infrastructure to be rolled out and would be very expensive, require a lot of material, investment, would not be scalable, and would require each new energy source to be connected to the system by a direct link to the costumer. This would ensure that origin of energy is always known, it would be hard to give an uptime guarantee, but it is feasible.

#### Smaller trading zones
Another strategy would be to divide the trading zones into smaller zones, and then use the energy supplied in each zone across each energy carrier 

#### Granular Certificates

| Description | Method | Organisation | Accounting method |
| --- | --- | --- | --- |
| Guarantees of Origin (GO) | Yearly certification | AIB | MWh (volume based) |
| Granular Certificates | Hourly certification (or less) | Energy Track and Trace / Energytag / etc | Wh (discretely time-based) |

