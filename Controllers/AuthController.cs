using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SymmetricSecurityKey _key;

    public AuthController()
    {
        // Zw24kielsneraeneysleopsExfg
        // _key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("your-super-secret-key-32-chars-long-at-least"));

        // your-super-secret-key-32-chars-long-at-least - used to sign a access token 
        // Purpose: This is used only for signing the new access token that your API sends back to the frontend after 
        // successful exchange.
        _key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("YourSuperSecretKeyForValidation256BitsOrMore"));
    }

    [HttpGet("generate-test")]
    public IActionResult GenerateTestToken()
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        // incoming-jwt-secret-key - external prvoider provided key
        // Purpose: This is used only for validating the incoming JWT token from an external authentication provider (
        // e.g., Auth0, Okta, or another service that issued the original token to your frontend).
        var incomingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("IncomingJwtSecretKeyForValidation256BitsOrMore"));
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
            new Claim(ClaimTypes.NameIdentifier, "testuser"),
            new Claim(ClaimTypes.Role, "Admin")
        }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(incomingKey, SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return Ok(new { TestToken = tokenHandler.WriteToken(token) });
    }
    [HttpGet("generate-test-nonadmin")]
    public IActionResult GenerateTestTokenForNonAdmin()
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        // incoming-jwt-secret-key - external prvoider provided key
        // Purpose: This is used only for validating the incoming JWT token from an external authentication provider (
        // e.g., Auth0, Okta, or another service that issued the original token to your frontend).
        var incomingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("IncomingJwtSecretKeyForValidation256BitsOrMore"));
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
            new Claim(ClaimTypes.NameIdentifier, "testuser"),
            new Claim(ClaimTypes.Role, "NonAdmin")
        }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(incomingKey, SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return Ok(new { TestToken = tokenHandler.WriteToken(token) });
    }



    [HttpPost("exchange")]
    public IActionResult ExchangeToken([FromBody] TokenRequest request)
    {
        if (string.IsNullOrEmpty(request.Token))
            return BadRequest("Token is required");

        // Validate incoming JWT (simple symmetric validation; adapt for your auth provider)
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,

            // incoming-jwt-secret-key - external prvoider provided key
            // Purpose: This is used only for validating the incoming JWT token from an external authentication provider (
            // e.g., Auth0, Okta, or another service that issued the original token to your frontend).

            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("IncomingJwtSecretKeyForValidation256BitsOrMore")),
            // Use your external JWT key
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };

        SecurityToken validatedToken;
        IEnumerable<Claim> claims;
        try
        {
            var principal = tokenHandler.ValidateToken(request.Token, validationParameters, out validatedToken);
            claims = principal.Claims;
            Console.WriteLine("Token validated successfully. Claims: " + string.Join(", ", claims.Select(c => $"{c.Type}={c.Value}")));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Validation failed: " + ex.Message);
            return Unauthorized("Invalid token: " + ex.Message);  // Expose error for testing (remove in prod)
        }

        // Map user claims to permissions (customize logic based on your incoming claims, e.g., roles)
        var permissions = new List<Claim>
        {
            new Claim("canAccessDashboard", "true"), // Example: always grant dashboard for simplicity
            new Claim("canAccessMoneyIn", claims.FirstOrDefault(c => c.Type == ClaimTypes.Role && c.Value == "Admin") != null ? "true" : "false"),
            new Claim("canAccessMoneyOut", claims.FirstOrDefault(c => c.Type == ClaimTypes.Role && c.Value == "Treasurer") != null ? "true" : "false")
        };

        // Create new access token (15 min expiry)
        var newTokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims.Concat(permissions)),
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256Signature)
        };

        var newToken = tokenHandler.CreateToken(newTokenDescriptor);
        var newJwt = tokenHandler.WriteToken(newToken);

        return Ok(new { AccessToken = newJwt });
    }
}

public class TokenRequest
{
    public string Token { get; set; } = string.Empty;
}