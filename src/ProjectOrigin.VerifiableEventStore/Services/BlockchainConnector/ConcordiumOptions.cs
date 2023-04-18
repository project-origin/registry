using System.ComponentModel.DataAnnotations;

public class ConcordiumOptions
{
    [Required, Url]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(256, MinimumLength = 1)]
    public string AuthenticationToken { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string AccountAddress { get; set; } = string.Empty;

    [Required, StringLength(64)]
    public string AccountKey { get; set; } = string.Empty;
}
