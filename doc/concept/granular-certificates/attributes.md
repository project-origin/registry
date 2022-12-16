# Attributes

A GC has a series of attributes, these are currently spit into two categories,
[primary](#primary-attributes) and [secondary](#secondary-attributes).

## Primary attributes

**Primary attributes** are used by the [registry](../registry.md)
to perform verification of commands performed.

### CertificateID

Is a [Federated Certificate ID](federated-certifate-id.md), it is the unique identifier for a certificate.
It consist of two parts
- RegistryId: of the [registry](../registry.md) holding the certificate.
- StreamId: Is the unique id of the certificate, it is a Uuid4.

### GSRN

A GSRN (Global Service Relation Number) is an unique identifier for a meter.

To ensure the privacy and hide the relations between GCs the GSRN value is hidden with the help
of a [Pedersen Commitment](../pedersen-commitments.md)

### Grid Area

The term Grid area was chosen instead of Price Area, the reason for this was to not thightly couple
the system with the current price-zones. Currently the Grid areas in use is the price areas,
but this could be subject to change at a later point in time.

Each Issuing body has a number of areas assigned to them, and only they are able to issue
valid GCs for these areas, and only their private key is allowed to sign issueing commands.

The grid area is used to enforce rules on the registry about how one can claim production GC
to a consumption GC.

### Period

The period of a GC describes the period of time in between the energy was produced/consumed.

The period consists of two values, a **Start** and **End** timestamp in **unix format**.

Start is considered inclusive and end exclusive,
so as to represent 1 hole whour from 10 to 11 on the 5. january 2022,
the values would contain the following:

|       | Iso8601           | Unix       |
| ----- | ----------------- | ---------- |
| Start | 2022-01-05T10:00Z | 1641376800 |
| End   | 2022-01-05T11:00Z | 1641380400 |

### Quantity

The quantity attribute describes a whole number in **Wh** of the energy flowing through the meter
in the period specified.

To ensure the privacy and hide the quantity, the value is hidden with the help
of a [Pedersen Commitment](../pedersen-commitments.md)

---

## Secondary attributes

**Secondary attributes** are information carrying attributes where the registry in agnostic
about the content of the attributes.

### AIB TechCode and FuelCode (production only)

Contains a tech and fuel code, as described in the [AIB Fact sheet 5](https://www.aib-net.org/sites/default/files/assets/eecs/facts-sheets/AIB-2019-EECSFS-05%20EECS%20Rules%20Fact%20Sheet%2005%20-%20Types%20of%20Energy%20Inputs%20and%20Technologies%20-%20Release%207.7%20v5.pdf)
This standard can describe which type of generation unit and what fuel was used in the production of the energy.

Since there was an existing and wellknown standard to describe this, ProjectOrigin chose to follow this standard.

### More to come

As the ProjectOrigin Registry is currently in PreAlpha all of these has yet to be defined,
and might be left open as a dictionary for the issuing body to choose what properties to include,
since the registry itself can be agnostic about the content.
