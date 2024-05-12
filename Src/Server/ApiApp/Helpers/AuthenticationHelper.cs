using ApiApp.Configuration;
using DataAccess.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCryptNet = BCrypt.Net.BCrypt;

namespace ApiApp.Helpers;

public interface IAuthenticationHelper
{
    string GenerateAccessToken(UserModel user);
    string GeneratePasswordHash(string password, string salt);
    string GenerateRefreshToken(UserModel user);
    string GenerateSalt();
    int ValidateRefreshTokenAndGetUserId(string? refreshToken);
}

public class AuthenticationHelper : IAuthenticationHelper
{
    public readonly JwtConfiguration _jwtConfiguration;

    public AuthenticationHelper(IOptions<JwtConfiguration> jwtConfiguration)
    {
        _jwtConfiguration = jwtConfiguration.Value;
    }

    public string GenerateSalt()
    {
        return BCryptNet.GenerateSalt();
    }

    public string GeneratePasswordHash(string password, string salt)
    {
        return  BCryptNet.HashPassword(password, salt);
    }

    /*
    public static bool VerifyBcryptHash(string password, string storedHash, string storedSalt)
    {
        if (string.IsNullOrEmpty(password)
            || string.IsNullOrEmpty(storedHash)
            || string.IsNullOrEmpty(storedSalt))
        {
            return false;
        }

        // Generate the hash using BCrypt and the stored salt
        string generatedHash = BCryptNet.HashPassword(password, storedSalt);

        // Compare the generated hash with the stored hash
        return storedHash == generatedHash;
    }
    */

    public string GenerateAccessToken(UserModel user)
    {
        return GenerateJwtTokenWithClaims(
            DateTime.Now.AddMinutes(15),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName)
        );
    }

    public string GenerateRefreshToken(UserModel user)
    {
        return GenerateJwtTokenWithClaims(
            DateTime.Now.AddSeconds(15),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("Refresh-Token", true.ToString())
        );
    }

    private string GenerateJwtTokenWithClaims(DateTime expires, params Claim[] claims)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfiguration.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
          _jwtConfiguration.Issuer,
          _jwtConfiguration.Audience,
          claims,
          expires: expires,
          signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public int ValidateRefreshTokenAndGetUserId(string? refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
            throw new ArgumentNullException(nameof(refreshToken));

        var tokenHandler = new JwtSecurityTokenHandler();

        //var token = tokenHandler.ReadJwtToken(refreshToken);

        tokenHandler.ValidateToken(refreshToken, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfiguration.Key)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = _jwtConfiguration.Issuer,
            ValidAudience = _jwtConfiguration.Audience,
            ValidateLifetime = false
        }, out SecurityToken validatedToken);

        //expire 
        if (validatedToken.ValidTo < DateTime.UtcNow)
            throw new Exception("Token expired");

        var jwtToken = (JwtSecurityToken)validatedToken;

        if (jwtToken == null || !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        var userId = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            throw new SecurityTokenException("Invalid token");

        return int.Parse(userId);

    }
}