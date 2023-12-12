using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using AutoFixture;
using FluentAssertions;
using Google.Protobuf;
using ProjectOrigin.Electricity.Server.Models;
using ProjectOrigin.Electricity.Server.Services;
using Xunit;

namespace ProjectOrigin.Electricity.Tests;

public class ModelHydraterTests
{
    [Fact]
    public void HydrateModel_ReturnsValidGranularCertificate_ForIssuedEvent()
    {
        var modelHydrater = new ElectricityModelHydrater();

        var fixture = new Fixture();

        var model = modelHydrater.HydrateModel<GranularCertificate>(new List<object>
        {
            fixture.Create<V1.IssuedEvent>()
        });

        Assert.NotNull(model);
        Assert.IsType<GranularCertificate>(model);
    }

    [Fact]
    public void HydrateModel_ReturnsValidGranularCertificate_ForMultipleEvents()
    {
        var modelHydrater = new ElectricityModelHydrater();

        var fixture = new Fixture();

        var issuedEvent = fixture.Create<V1.IssuedEvent>();
        var transferredEvent = fixture.Create<V1.TransferredEvent>();
        transferredEvent.SourceSliceHash = ByteString.CopyFrom(SHA256.HashData(issuedEvent.QuantityCommitment.Content.ToByteArray()));

        var model = modelHydrater.HydrateModel<GranularCertificate>(new List<object>
        {
            issuedEvent,
            transferredEvent,
        });

        Assert.NotNull(model);
        Assert.IsType<GranularCertificate>(model);
    }

    [Fact]
    public void CreateModel_ThrowsNotSupportedException_ForUnsupportedEvent()
    {
        var modelHydrater = new ElectricityModelHydrater();

        var fixture = new Fixture();

        var method = () => modelHydrater.HydrateModel<GranularCertificate>(new List<object>
        {
            fixture.Create<V1.AllocatedEvent>()
        });

        Assert.Throws<NotSupportedException>(method).Message.Should().Be("Event ”ProjectOrigin.Electricity.V1.AllocatedEvent” not supported to create model");
    }
}
