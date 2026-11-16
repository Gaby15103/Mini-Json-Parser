namespace MiniJsonParser.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void DeserializeAndSerialize_ReadonlyRecordStruct_UsesParameterizedConstructor()
    {
        string json = """{"client_id":"app-99","grant_type":"refresh_token"}""";
        var payload = JsonSerializer.Deserialize<OAuthPayload>(json);

        Assert.That(payload.ClientId, Is.EqualTo("app-99"));
        Assert.That(payload.GrantType, Is.EqualTo("refresh_token"));

        var snakeOptions = new JsonSerializerOptions { NamingPolicy = JsonNamingPolicy.SnakeCase };
        string backToJson = JsonSerializer.Serialize(payload, snakeOptions);
        Assert.That(backToJson, Is.EqualTo("""{"client_id":"app-99","grant_type":"refresh_token"}"""));
    }

    [Test]
    public void Deserialize_LargeLongAndDecimal_PreservesExactPrecision()
    {
        string json = """{"snowflakeId":9007199254740993,"balance":123456.789,"retryCount":null,"optionalAge":25}""";
        var data = JsonSerializer.Deserialize<PrecisionModel>(json);

        Assert.That(data.SnowflakeId, Is.EqualTo(9007199254740993L));
        Assert.That(data.Balance, Is.EqualTo(123456.789m));
        Assert.That(data.RetryCount, Is.Null);
        Assert.That(data.OptionalAge, Is.EqualTo(25));
    }

    [Test]
    public void Deserialize_NestedObjectsArraysListsAndEnums_MapsRecursively()
    {
        string json = """
                      {
                        "userId": 7,
                        "role": "Admin",
                        "secondaryRole": 2,
                        "tags": ["dotnet", "reflection", "json"],
                        "scores":[10,20,30],
                        "profile": { "city": "Montreal", "verified": true }
                      }
                      """;

        var account = JsonSerializer.Deserialize<UserAccount>(json);

        Assert.That(account.UserId, Is.EqualTo(7));
        Assert.That(account.Role, Is.EqualTo(UserRole.Admin));
        Assert.That(account.SecondaryRole, Is.EqualTo(UserRole.Moderator));
        Assert.That(account.Tags, Is.EqualTo(new[] { "dotnet", "reflection", "json" }));
        Assert.That(account.Scores, Is.EqualTo(new[] { 10, 20, 30 }));
        Assert.That(account.Profile, Is.Not.Null);
        Assert.That(account.Profile!.City, Is.EqualTo("Montreal"));
        Assert.That(account.Profile.Verified, Is.True);
    }

    [Test]
    public void Deserialize_EmptyCollectionsAndEmptyNestedObject_DoesNotThrow()
    {
        string json = """{"userId":1,"tags":[],"scores":[],"profile":{}}""";
        var account = JsonSerializer.Deserialize<UserAccount>(json);

        Assert.That(account.Tags, Is.Empty);
        Assert.That(account.Scores, Is.Empty);
        Assert.That(account.Profile, Is.Not.Null);
        Assert.That(account.Profile!.City, Is.Empty);
    }

    [Test]
    public void Serialize_CircularReference_ThrowsInvalidOperationException()
    {
        var nodeA = new Node { Name = "A" };
        var nodeB = new Node { Name = "B", Next = nodeA };
        nodeA.Next = nodeB;

        Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(nodeA));
    }

    [Test]
    public void Deserialize_NonObjectRoot_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<LoginRequest>("[1, 2, 3]"));
    }
    
    public struct LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool RememberMe;
    }

    public readonly record struct OAuthPayload(
        string ClientId,
        string GrantType
    );

    public class PrecisionModel
    {
        public long SnowflakeId { get; set; }
        public decimal Balance { get; set; }
        public int? RetryCount { get; set; }
        public int? OptionalAge { get; set; }
    }

    public enum UserRole
    {
        Guest = 0,
        Admin = 1,
        Moderator = 2
    }

    public class UserAccount
    {
        public int UserId { get; set; }
        public UserRole Role { get; set; }
        public UserRole SecondaryRole { get; set; }
        public List<string> Tags { get; set; } = new();
        public int[] Scores { get; set; } = Array.Empty<int>();
        public UserProfile? Profile { get; set; }
    }

    public class UserProfile
    {
        public string City { get; set; } = "";
        public bool Verified { get; set; }
    }

    public class Node
    {
        public string Name { get; set; } = "";
        public Node? Next { get; set; }
    }
}