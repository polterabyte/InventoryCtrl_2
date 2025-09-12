namespace Inventory.Shared;

public class LoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}
public class RefreshRequest
{
    public required string Username { get; set; }
    public required string RefreshToken { get; set; }
}
public class RegisterRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}