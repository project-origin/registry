# Overview of Project-Origin

## Certification of Energy
_Why bother at all_? one might ask, and argue that the energy supplied in the socket is electrons and its origin whatever is available at the given time, it is all in the residual mix, if I want solar or wind specifically I can simply shift my consumption to hours where there is a high degree of such sources in the residual mix ? A feasible strategy - yes, practical definately not.

However, leveraging that electricity meters can provide extensive amounts of data - down to an interval of mere minutes - we can separate and document the total amount of electricity provided to the grid at a given time in terms of amounts and their origin at high temporal granularity, on a basis aligned with the spot market. 

Such documentation enables us to design and create a certification system, making it possible for consumers to prove the source of their consumed electricity at specific time intervals. 
This certification system is extremely powerful as it provides trustworthy proofs, and given the [high demand for green energy](https://www2.deloitte.com/content/dam/Deloitte/us/Documents/energy-resources/us-eri-renewable-energy-outlook-2023.pdf) creates incentives to prioritize supply from renewable sources, while effectively preventing greenwashing. 

The scope of a trustworthy certification system like described above, can be useful in markets for various types of  energy. Additionally, a shared technical solution for certification across energy types creates the basis for coupling across sectors by supporting hybrid and Power-to-X technologies.

This is the basis of Project-Origin - creating a trustworthy, secure certification system that can be shared across energy types, by implementing the fundamental technical core functionality. 
The implementation is provided as available open-source software, to allow anyone to use it, and build any necessary functionality on top to create a certification system able to trace their specific resource(s) in transactions.   


![](/resources/graphics/ETTvalue.drawio.png)

## How it has been done so far ?
As the shares of renewables in the energy mix of some countries are rising towards 30%, the need for a system to certify the energy supplied in a trustworthy and transparent way becomes increasingly important. 
Currently, certification is done using [Guarantees of Origin](https://en.energinet.dk/Energy-data/Guarantees-of-origin-el-gas-hydrogen/) (GOs), issued on a monthly basis to an electronic register, and can be traded and/or cancelled up to 12 months after being issued. 
This system has been in place for a long time and is generally well understood. 
However, the GO certification scheme has received lots of criticism, especially evolving around the scheme [lacking credibility](https://ieeexplore.ieee.org/abstract/document/5311433), [lacking national implementations of disclosure regulation](https://www.oeko.de/fileadmin/oekodoc/Reliable-Disclosure-in-Europe-Status-Improvements-and-Perspectives.pdf), and having [little to no effect in terms of accelerating investments](https://akjournals.com/view/journals/204/41/4/article-p487.xml?body=contentsummary-24716) in renewable energy production.  
The drawbacks of the GO certification system will be elaborated below. 

### Guarantees of Origin
Guarantees of Origin (GOs) are issued monthly as 1 MWh fixed-volume certificates, which can be traded up to one year after being issued. 
The rules specifying the format of GOs are provided in the [EN 16325](https://standards.globalspec.com/std/9969735/EN%2016325) standard developed by the European Committee for Standardisation (CEN) and the European Committee for Electrotechnical Standardisation (CENELEC). 
The GO certification system is regulated by the [Renewable Energy Directive](https://energy.ec.europa.eu/topics/renewable-energy/renewable-energy-directive-targets-and-rules/renewable-energy-directive_en) (RED-II). 

the [Association of Issuing Bodies](https://www.aib-net.org/) (AIB) is the central authority for the GO system in Europe, operating a central hub for trading the certificates following the EN 16325 standard. 

The procedure for trading GOs is as follows:

1. A producer of energy requests a certificate for each MWh of energy produced, and a third party issuer, compliant with the central authority (AIB), issues the GO(s) to the producer
2. The GO(s) is/are sold to a supplier
3. The supplier sells the GO(s) to a consumer
4. The consumer can then "use" the GO, by claiming, or [cancelling](https://en.energinet.dk/Energy-data/Guarantees-of-origin-el-gas-hydrogen/#accordion-cancellation), effectively attributing consumption of the renewable energy amount specified by the GO to the consumer.

To summarize, the existing GO system is based on the following principles:
- Yearly certification
- Volume based accounting
- Exchange of certificates between parties through a centralised system AIB-hub
- The certificate is a tradeable asset
- No direct physical link between the certificate and the energy supplied is required
#### Challenges  
At times, certain grid areas experience a supply by renewables that is [bigger than the grid capacity](https://www.caiso.com/documents/curtailmentfastfacts.pdf), which brings a number of [challenges to the existing, physical grid](https://www.rff.org/publications/explainers/renewables-101-integrating-renewables/): 
- It becomes [difficult to stabilize the grid frequency](https://www.engineering.com/story/grid-frequency-stability-and-renewable-power) in the electricity grid.
- Insufficient capacity of the existing grid creates [bottlenecks](https://www.zerohedge.com/energy/grid-bottlenecks-could-derail-europes-renewable-energy-boom), limiting transportation between certain areas 
- [Less centralized load balancing](https://research.rug.nl/en/publications/local-balancing-of-the-electricity-grid-in-a-renewable-municipali) might be necessary 
- Makes it hard to validate the actual energy supplied to power-to-x applications and the origin of the losses in the process
#### Drawbacks
Note that this section is primarily focused on the drawbacks of GOs in the electricity market. 
However, several of the drawbacks described applies to other energy markets as well. 

Despite the intentions behind the GO certification system, there are several disadvantages to this system. 
Firstly, the low price of the certificates, which is [driven down](https://www.sciencedirect.com/science/article/abs/pii/S0301421504002423) especially by Norway flooding the market with GOs, based on their extensive amounts of hydro power. 
This low price does [not generate enough economy](https://ideas.repec.org/a/eco/journ2/2018-05-21.html#:~:text=Factors%20Affecting%20the%20Evolution%20of%20Renewable%20Electricity%20Generating,Citation%20%C3%81kos%20Hamburger%20%26%20G%C3%A1bor%20Harangoz%C3%B3%2C%202018.%20) to create incentives for improvements and investments in renewable energy production facilities.

The physical flow of electricity cannot be tracked, making it difficult to validate the information in the certificates. 
A major issue arising from this is [mistrust in the GOs](https://www.sciencedirect.com/science/article/abs/pii/S0301421510006932) from consumers, who generally struggle to understand how tracing is done without actual coupling to the actual electricity.  
Additionally, the price of long distance transportation is not taken into account in the certification system. 
The interconnector capacities are also [highly exploited](https://akjournals.com/view/journals/204/41/4/article-p487.xml?body=contentsummary-24716), leading to the traded quantities of electricity attributes being much higher than what could ever be realized in the present interconnector capacities.

[//]: # (TODO: Maybe a small paragraph on no effect on the binding target of RES share in the energy mix. The GOs are not included in the statistics calculated for this (https://akjournals.com/view/journals/204/41/4/article-p487.xml?body=contentsummary-24716)) .

Cyprus, Iceland, and Ireland are members of AIB but have no physical interconnection to other AIB countries. 
Cyprus and Iceland have no interconnectors at all. No physical flows are possible from or to these countries. 
However, Iceland is a significant exporter of GOs among these countries. 
In some cases, GO trade even occurs to far away lands such as Australia, Brazil, Chile, China, India, Japan, Qatar, Thailand, Saudi Arabia, the United Arab Emirates, and the United States, with no existing interconnector capacities (See [this paper](https://akjournals.com/view/journals/204/41/4/article-p487.xml?body=contentsummary-24716) for more information).

This creates a serious inconsistency in the certificates and raises the question, if we cannot document the share of renewable energy in the energy mix in a trustworthy and transparent way, how can we then reach binding targets for renewable energy shares in the energy mix?


The above is by no means an extensive and exhaustive analysis of the existing Guarantees of Origin system, but it is a good starting point for understanding the system. 
Tackling the challenges of the existing system is the basis for the [Energy Track and Trace](https://energytrackandtrace.com/) project  and [Project-Origin](https://github.com/project-origin). 

The following section adresses potential strategies on how to obtain an improved, more trustworthy mechanism for an energy certification system.

## Strategies for updating the certification system
Several strategies could be applied to the infrastructure to tackle the challenges of the existing system. 

#### Direct infrastructure
Simply adding a direct, physical link between the energy supplied and consumer would be a simple, straightforward solution. 
However, it would require a lot of infrastructure to be rolled out, requiring a lot of material, and investments, making such solution unscalable and expensive. 
Of course, this solution would ensure that the origin of energy is always known, but given the extreme amount of physical infrastructure needed makes it unfeasible.

#### Smaller trading zones
Another strategy would be to divide the trading zones into smaller zones, and then use the energy supplied in each zone across each energy carrier 

#### Granular Certificates

| Description | Method | Organisation | Accounting method |
| --- | --- | --- | --- |
| Guarantees of Origin (GO) | Yearly certification | AIB | MWh (volume based) |
| Granular Certificates | Hourly certification (or less) | Energy Track and Trace / Energytag / etc | Wh (discretely time-based) |

