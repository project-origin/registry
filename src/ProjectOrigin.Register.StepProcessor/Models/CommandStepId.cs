namespace ProjectOrigin.Register.StepProcessor.Models;

public record CommandStepId(byte[] RequestHash)
{
    public virtual bool Equals(CommandStepId? other)
    {
        if (other is null) return false;

        return RequestHash.SequenceEqual(other!.RequestHash);
    }

    public override int GetHashCode()
    {
        var hc = RequestHash.Length;
        foreach (int val in RequestHash)
        {
            hc = unchecked(hc * 314159 + val);
        }
        return hc;
    }
}
