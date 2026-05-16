using Duende.IdentityModel.Client;

namespace ZeroX2C.Blog.API.Modules.Users.Auth;

public sealed class SteamOpenIdClient(HttpClient httpClient) : ISteamOpenIdClient
{
    private const string SteamOpenIdEndpoint = "https://steamcommunity.com/openid/login";
    private const string OpenIdNamespace = "http://specs.openid.net/auth/2.0";
    private const string IdentifierSelect = "http://specs.openid.net/auth/2.0/identifier_select";
    private const string SteamClaimedIdPrefix = "https://steamcommunity.com/openid/id/";

    public string CreateAuthenticationUrl(string returnTo, string realm)
    {
        var parameters = new Parameters
        {
            { "openid.ns", OpenIdNamespace },
            { "openid.mode", "checkid_setup" },
            { "openid.return_to", returnTo },
            { "openid.realm", realm },
            { "openid.identity", IdentifierSelect },
            { "openid.claimed_id", IdentifierSelect },
        };

        return new RequestUrl(SteamOpenIdEndpoint).Create(parameters);
    }

    public async Task<SteamOpenIdValidationResult> ValidateCallbackAsync(
        SteamOpenIdCallback callback,
        string expectedReturnTo,
        CancellationToken cancellationToken
    )
    {
        if (!TryGetParameter(callback, "openid.mode", out var mode) || mode != "id_res")
        {
            return SteamOpenIdValidationResult.Invalid();
        }

        if (
            !TryGetParameter(callback, "openid.ns", out var ns)
            || !string.Equals(ns, OpenIdNamespace, StringComparison.Ordinal)
        )
        {
            return SteamOpenIdValidationResult.Invalid();
        }

        if (
            !TryGetParameter(callback, "openid.op_endpoint", out var opEndpoint)
            || !string.Equals(opEndpoint, SteamOpenIdEndpoint, StringComparison.Ordinal)
        )
        {
            return SteamOpenIdValidationResult.Invalid();
        }

        if (
            !TryGetParameter(callback, "openid.return_to", out var returnTo)
            || !string.Equals(returnTo, expectedReturnTo, StringComparison.Ordinal)
        )
        {
            return SteamOpenIdValidationResult.Invalid();
        }

        if (
            !TryGetParameter(callback, "openid.claimed_id", out var claimedId)
            || !TryExtractSteamId(claimedId, out var steamId)
        )
        {
            return SteamOpenIdValidationResult.Invalid();
        }

        if (
            !TryGetParameter(callback, "openid.identity", out var identity)
            || !string.Equals(identity, claimedId, StringComparison.Ordinal)
        )
        {
            return SteamOpenIdValidationResult.Invalid();
        }

        var verificationParameters = new Parameters(
            callback.Parameters
                .Where(parameter => parameter.Key.StartsWith("openid.", StringComparison.Ordinal))
                .Where(parameter => parameter.Key != "openid.mode")
                .Select(parameter => new KeyValuePair<string, string>(parameter.Key, parameter.Value))
        )
        {
            { "openid.mode", "check_authentication" },
        };

        using var verificationContent = new FormUrlEncodedContent(verificationParameters);
        using var response = await httpClient.PostAsync(
            SteamOpenIdEndpoint,
            verificationContent,
            cancellationToken
        );
        if (!response.IsSuccessStatusCode)
        {
            return SteamOpenIdValidationResult.Invalid();
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var isValid = responseBody
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(line => string.Equals(line, "is_valid:true", StringComparison.OrdinalIgnoreCase));

        return isValid
            ? SteamOpenIdValidationResult.Valid(steamId)
            : SteamOpenIdValidationResult.Invalid();
    }

    private static bool TryGetParameter(
        SteamOpenIdCallback callback,
        string name,
        out string value
    )
    {
        if (callback.Parameters.TryGetValue(name, out var parameterValue))
        {
            value = parameterValue;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryExtractSteamId(string claimedId, out string steamId)
    {
        if (!claimedId.StartsWith(SteamClaimedIdPrefix, StringComparison.OrdinalIgnoreCase))
        {
            steamId = string.Empty;
            return false;
        }

        steamId = claimedId[SteamClaimedIdPrefix.Length..];
        return steamId.Length > 0 && steamId.Length <= 32 && steamId.All(char.IsDigit);
    }
}
