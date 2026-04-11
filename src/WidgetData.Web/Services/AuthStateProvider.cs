using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace WidgetData.Web.Services;

public class AuthStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public void SetUser(string token)
    {
        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
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
            return json?.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString())) ?? Enumerable.Empty<Claim>();
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
}
