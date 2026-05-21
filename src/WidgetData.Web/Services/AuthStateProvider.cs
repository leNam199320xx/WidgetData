using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace WidgetData.Web.Services;

public class AuthStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public bool IsAuthenticated => _currentUser.Identity?.IsAuthenticated == true;

    public void SetUser(string token)
    {
        var claims = ParseClaimsFromJwt(token).ToList();
        if (IsExpired(claims))
        {
            ClearUser();
            return;
        }

        var identity = new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role);
        _currentUser = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    public void ClearUser()
    {
        _currentUser = new(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(new AuthenticationState(_currentUser));

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length != 3)
                return Enumerable.Empty<Claim>();

            var payload = parts[1];
            var padded = payload.Length % 4 == 0 ? payload : payload + new string('=', 4 - payload.Length % 4);
            var bytes = Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'));
            var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(bytes);
            if (json == null)
                return Enumerable.Empty<Claim>();

            var claims = new List<Claim>();
            foreach (var (key, value) in json)
            {
                var claimType = NormalizeClaimType(key);
                if (value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in value.EnumerateArray())
                        claims.Add(new Claim(claimType, item.ToString()));
                }
                else
                {
                    claims.Add(new Claim(claimType, value.ToString()));
                }
            }

            return claims;
        }
        catch (FormatException)
        {
            return Enumerable.Empty<Claim>();
        }
        catch (JsonException)
        {
            return Enumerable.Empty<Claim>();
        }
    }

    private static string NormalizeClaimType(string claimType) => claimType switch
    {
        "sub" => ClaimTypes.NameIdentifier,
        "email" => ClaimTypes.Email,
        "unique_name" => ClaimTypes.Name,
        "role" => ClaimTypes.Role,
        _ => claimType
    };

    private static bool IsExpired(IEnumerable<Claim> claims)
    {
        var exp = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        return long.TryParse(exp, out var seconds)
            && DateTimeOffset.FromUnixTimeSeconds(seconds) <= DateTimeOffset.UtcNow;
    }
}
