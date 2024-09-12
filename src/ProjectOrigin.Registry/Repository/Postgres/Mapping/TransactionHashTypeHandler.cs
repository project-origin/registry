using System.Data;
using Dapper;
using ProjectOrigin.Registry.Repository.Models;

namespace ProjectOrigin.Registry.Repository.Postgres.Mapping;

public class TransactionHashTypeHandler : SqlMapper.TypeHandler<TransactionHash>
{
    public override TransactionHash Parse(object value)
    {
        return new TransactionHash((byte[])value);
    }

    public override void SetValue(IDbDataParameter parameter, TransactionHash? value)
    {
        parameter.Value = value?.Data;
    }
}
