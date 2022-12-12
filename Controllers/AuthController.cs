using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using JwtToken.DTOs;
using JwtToken.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace JwtToken.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    static User createdUser = new();
    private readonly IConfiguration _configuration;
    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<ActionResult<User>> Register(UserDTO userDTO)
    {
        CreatePasswordHash(userDTO.Password, out byte[] passwordHash, out byte[] passwordSalt);

        createdUser.Email = userDTO.Email;
        createdUser.PasswordHash = passwordHash;
        createdUser.PasswordSalt = passwordSalt;

        return Ok(createdUser);
    }

    [HttpPost("login")]
    public async Task<ActionResult<string>> Login(UserDTO userDTO)
    {
        if (createdUser.Email != userDTO.Email)
            return NotFound("User not found");

        if (!VerifyPasswordHash(userDTO.Password, createdUser.PasswordHash, createdUser.PasswordSalt))
            return BadRequest("Wrong password");

        string token = CreateToken(createdUser);

        return Ok(token);
    }

    private string CreateToken(User createdUser)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, createdUser.Email)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("Settings:Token").Value));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(20),
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return jwt;

    }

    private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        using var hmac = new HMACSHA512();

        passwordSalt = hmac.Key;
        passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }

    private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
    {
        using var hmac = new HMACSHA512(passwordSalt);

        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

        return computedHash.SequenceEqual(passwordHash);
    }
}
