namespace Requests.Api.Http;

/// <summary>
/// The name of the user making the request, from the X-User-Name header. There is no authentication yet;
/// in a real system this would come from the authenticated user (e.g. a JWT claim).
/// </summary>
public sealed class CurrentUserAccessor
{
    public const string HeaderName = "X-User-Name";
    public const string Anonymous = "anonymous";

    /// <summary>Matches the ChangedBy column length.</summary>
    public const int MaxLength = 100;

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserName
    {
        get
        {
            var raw = _httpContextAccessor.HttpContext?.Request.Headers[HeaderName].ToString();
            if (string.IsNullOrEmpty(raw))
            {
                return Anonymous;
            }

            // Browsers refuse non-ASCII header values (e.g. Hebrew names), so the client sends the value
            // percent-encoded (encodeURIComponent). Invalid escape sequences are left as they are.
            var value = Uri.UnescapeDataString(raw).Trim();
            if (value.Length == 0)
            {
                return Anonymous;
            }

            return value.Length > MaxLength ? value[..MaxLength] : value;
        }
    }
}
