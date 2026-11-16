using MiniJsonParser;

var loginPayload = new LoginRequest
{
    Username = "gaby15103",
    Password = "secret_password",
    RememberMe = true
};

string jsonBody = JsonSerializer.Serialize(loginPayload);
Console.WriteLine(jsonBody);

string apiResponse = """{"access_token":"jwt_xyz_123","expires_in":3600,"internalDebug":"skip"}""";
var auth = JsonSerializer.Deserialize<AuthTokenResponse>(apiResponse);
Console.WriteLine($"Token: {auth.AccessToken}, Expires: {auth.ExpiresIn}");

public struct LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
    public bool RememberMe;
}

public struct AuthTokenResponse
{
    public string AccessToken { get; set; }
    public int ExpiresIn { get; set; }
    public string? InternalDebug { get; set; }
}