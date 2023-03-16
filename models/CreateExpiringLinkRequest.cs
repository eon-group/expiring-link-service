namespace EON.Models
{
  public class CreateExpiringLinkRequest
  {
    // URL to redirect to if the link is valid
    public string url;
    // Minutes from creation time until link expires
    public int expiresIn;
    // If the link should expire immediately after being accessed
    public bool expiresOnAccess;
    // Custom Url to redirect to, if the link is expired
    public string expiredRedirectUrl;
  }
}