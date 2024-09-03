using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ProjectOrigin.Registry.Server.Options;
using Xunit;

namespace ProjectOrigin.Registry.IntegrationTests
{
    public class OtlpOptionsTests
    {

        [Fact]
        public void OtlpOptions_CanBeInstantiated()
        {
            var options = new OtlpOptions
            {
                Enabled = false
            };
            Assert.NotNull(options);
        }

        [Fact]
        public void Validate_GeneratesError_WhenEnabledTrueAndEndpointNull()
        {
            var options = new OtlpOptions { Enabled = true, Endpoint = null };
            var results = options.Validate(new ValidationContext(options)).ToList();
            Assert.NotEmpty(results);
        }

        [Fact]
        public void Validate_GeneratesNoError_WhenEnabledTrueAndEndpointIsValid()
        {
            var options = new OtlpOptions { Enabled = true, Endpoint = new Uri("http://localhost:4317") };
            var results = options.Validate(new ValidationContext(options)).ToList();
            Assert.Empty(results);
        }

        [Fact]
        public void Validate_GeneratesError_WhenEnabledTrueAndEndpointIsInvalidScheme()
        {
            var options = new OtlpOptions { Enabled = true, Endpoint = new Uri("ftp://example.com") };

            var results = options.Validate(new ValidationContext(options)).ToList();

            Assert.NotEmpty(results);
        }


        [Fact]
        public void Validate_GeneratesError_WhenEnabledTrueAndEndpointHasUnsupportedScheme()
        {
            var options = new OtlpOptions { Enabled = true, Endpoint = new Uri("ftp://localhost:4317") };
            var results = options.Validate(new ValidationContext(options)).ToList();
            Assert.NotEmpty(results);
        }

        [Fact]
        public void Validate_GeneratesNoError_WhenEnabledFalseRegardlessOfEndpoint()
        {
            var options = new OtlpOptions { Enabled = false, Endpoint = null };
            var results = options.Validate(new ValidationContext(options)).ToList();
            Assert.Empty(results);
        }
    }
}
