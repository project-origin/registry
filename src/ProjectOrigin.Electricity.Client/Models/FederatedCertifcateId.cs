namespace ProjectOrigin.Electricity.Client.Models;

/// <summary>
/// A FederatedCertifcateId contains the unique CertificateId of the <a href="xref:granular_certificate">Granular Certificate</a>,
/// aswell as the identifier for the registry that the <a href="xref:granular_certificate">Granular Certificate</a> lives on.
/// </summary>
public class FederatedCertifcateId
{
    /// <summary>
    /// The string identifier for the registry holding the certificate.
    /// </summary>
    public string Registry { get; }

    /// <summary>
    /// The unique id of the certificate.
    /// </summary>
    public Guid CertificateId { get; }

    /// <summary>
    /// Creates a FederatedCertifcateId.
    /// </summary>
    /// <param name="registry">The string identifier for the registry holding the certificate.</param>
    /// <param name="certificateId">The unique id of the certificate.</param>
    public FederatedCertifcateId(string registry, Guid certificateId)
    {
        Registry = registry;
        CertificateId = certificateId;
    }

    internal Register.V1.FederatedStreamId ToProto() => new Register.V1.FederatedStreamId()
    {
        Registry = Registry,
        StreamId = new Register.V1.Uuid()
        {
            Value = CertificateId.ToString()
        }
    };
}
