namespace ProjectOrigin.Register.LineProcessor.Models;

public record FederatedStreamId(
    string Registry,
    Guid StreamId)
{
    public static implicit operator FederatedStreamId(V1.FederatedStreamId proto)
    {
        return new FederatedStreamId(proto.Registry, Guid.Parse(proto.StreamId.Value));
    }

    public static implicit operator V1.FederatedStreamId(FederatedStreamId id)
    {
        return new V1.FederatedStreamId()
        {
            Registry = id.Registry,
            StreamId = new V1.Uuid()
            {
                Value = id.StreamId.ToString()
            }
        };
    }
}
