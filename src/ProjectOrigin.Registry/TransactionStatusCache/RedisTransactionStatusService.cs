using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Registry.Repository;
using ProjectOrigin.Registry.Repository.Models;
using StackExchange.Redis;

namespace ProjectOrigin.Registry.TransactionStatusCache;

public sealed class RedisTransactionStatusService : ITransactionStatusService
{
    private static readonly TimeSpan CacheTime = TimeSpan.FromMinutes(60);

    private readonly ILogger<RedisTransactionStatusService> _logger;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ITransactionRepository _transactionRepository;

    public RedisTransactionStatusService(
        ILogger<RedisTransactionStatusService> logger,
        IConnectionMultiplexer connectionMultiplexer,
        ITransactionRepository transactionRepository)
    {
        _logger = logger;
        _connectionMultiplexer = connectionMultiplexer;
        _transactionRepository = transactionRepository;
    }

    private static byte[] Encode(TransactionStatusRecord rec)
    {
        var msg = string.IsNullOrEmpty(rec.Message)
            ? Array.Empty<byte>()
            : Encoding.UTF8.GetBytes(rec.Message);

        var buf = new byte[1 + msg.Length];
        buf[0]  = (byte)rec.NewStatus;
        if (msg.Length > 0)
            Buffer.BlockCopy(msg, 0, buf, 1, msg.Length);

        return buf;
    }

    private static bool TryDecode(RedisValue blob, out TransactionStatusRecord record)
    {
        if (blob.IsNull)
        {
            record = null!;
            return false;
        }

        byte[]? bytes = blob;
        if (bytes is null || bytes.Length == 0)
        {
            record = null!;
            return false;
        }

        record = Decode(bytes);
        return true;
    }

    private static TransactionStatusRecord Decode(ReadOnlySpan<byte> blob)
    {
        if (blob.Length == 0)
            return new TransactionStatusRecord(TransactionStatus.Unknown, string.Empty);

        var status  = (TransactionStatus)blob[0];
        var message = blob.Length > 1
            ? Encoding.UTF8.GetString(blob[1..])
            : string.Empty;

        return new TransactionStatusRecord(status, message);
    }

    public async Task<TransactionStatusRecord> GetTransactionStatus(TransactionHash txHash)
    {
        var db   = _connectionMultiplexer.GetDatabase();
        var blob = await db.StringGetAsync((RedisKey)txHash);

        if (TryDecode(blob, out var cached))
            return cached;

        var dbStatus  = await _transactionRepository.GetTransactionStatus(txHash);
        var statusRec = new TransactionStatusRecord(dbStatus);

        _ = await db.StringSetAsync(
            (RedisKey)txHash,
            (RedisValue)Encode(statusRec),
            expiry: CacheTime,
            when: When.NotExists);

        return statusRec;
    }

    public async Task SetTransactionStatus(TransactionHash txHash,
        TransactionStatusRecord newRec)
    {
        _logger.LogTrace(
            "Setting transaction status for {transactionHash} to {newStatus}",
            txHash, newRec.NewStatus);

        var db      = _connectionMultiplexer.GetDatabase();
        var curBlob = await db.StringGetAsync((RedisKey)txHash);

        TransactionStatusRecord? curRec = null;
        var curFound = TryDecode(curBlob, out var decoded);
        if (curFound) curRec = decoded;

        if (newRec.NewStatus < (curRec?.NewStatus ?? TransactionStatus.Unknown))
        {
            var oldStatus = curRec?.NewStatus ?? TransactionStatus.Unknown;
            _logger.LogWarning(
                "Transaction {transactionHash} status in cache is {oldStatus} and is higher than {newStatus}, change aborted.",
                txHash, oldStatus, newRec.NewStatus);
            return;
        }

        const string script = @"
        local cur = redis.call('GET', KEYS[1])
        if not cur or string.byte(cur,1) <= tonumber(ARGV[1]) then
            redis.call('SET', KEYS[1], ARGV[2], 'PX', ARGV[3])
            return 1
        else
            return 0
        end";

        var added = (int)await db.ScriptEvaluateAsync(
            script,
            keys: [(RedisKey)txHash],
            values:
            [
                (int)newRec.NewStatus,
                (RedisValue)Encode(newRec),
                (long)CacheTime.TotalMilliseconds
            ]);

        if (added == 0)
        {
            var oldStatus = curRec?.NewStatus ?? TransactionStatus.Unknown;
            _logger.LogError(
                "Transaction {transactionHash} status was changed by another process while setting to {newStatus}, old state {cacheState}, change aborted.",
                txHash, newRec.NewStatus, oldStatus);
        }
    }
}
