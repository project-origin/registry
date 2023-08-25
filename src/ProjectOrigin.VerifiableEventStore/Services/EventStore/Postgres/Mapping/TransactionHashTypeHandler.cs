using System.Data;
using Dapper;
using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore.Postgres.Mapping;

public class TransactionHashTypeHandler : SqlMapper.TypeHandler<TransactionHash>
{
    public override TransactionHash Parse(object value)
    {
        return new TransactionHash((byte[])value);
    }

    public override void SetValue(IDbDataParameter parameter, TransactionHash value)
    {
        parameter.Value = value.Data;
    }
}
