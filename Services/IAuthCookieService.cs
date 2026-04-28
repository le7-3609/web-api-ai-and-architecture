namespace Services;

public interface IAuthCookieService
{
    void SetAuthCookies(string accessToken, string refreshToken);
    void DeleteAuthCookies();
    string? GetRefreshToken();
}
