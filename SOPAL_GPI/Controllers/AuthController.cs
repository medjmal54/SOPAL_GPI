using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPAL_GPI.Data;
using SOPAL_GPI.DTOs;
using SOPAL_GPI.Services;

namespace SOPAL_GPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IJwtRepository jwtRepository, IEmployeRepository employeRepository) : ControllerBase
    {
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginDTO dto)
        {
            var employe = await employeRepository.GetByIdAsync(dto.Matricule);
            if (employe is null)
                return Unauthorized("Matricule ou email incorrect.");

            if (!employe.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase))
                return Unauthorized("Matricule ou email incorrect.");

            if (employe.Actif == false)
                return Unauthorized("Votre compte est désactivé.");

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, employe.Matricule.ToString()),
                new(ClaimTypes.Name, $"{employe.Prenom} {employe.Nom}"),
                new(ClaimTypes.Email, employe.Email),
                new(ClaimTypes.Role, employe.Role ?? "User")
            };

            var accessToken = jwtRepository.GenerateAccessToken(claims);
            var refreshToken = jwtRepository.GenerateRefreshToken(claims);

            var accessCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = DateTime.UtcNow.AddMinutes(15)
            };
            if (!HttpContext.Request.IsHttps)
                accessCookieOptions.Secure = false;
            Response.Cookies.Append("jwt_access_token", accessToken, accessCookieOptions);

            var refreshCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(7)
            };
            if (!HttpContext.Request.IsHttps)
                refreshCookieOptions.Secure = false;
            Response.Cookies.Append("jwt_refresh_token", refreshToken, refreshCookieOptions);

            return Ok(new
            {
                matricule = employe.Matricule,
                nom = employe.Nom,
                prenom = employe.Prenom,
                role = employe.Role
            });
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public IActionResult Refresh()
        {
            var refreshToken = Request.Cookies["jwt_refresh_token"];
            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized();

            var principal = jwtRepository.ValidateRefreshToken(refreshToken);
            if (principal is null)
                return Unauthorized();

            var newAccessToken = jwtRepository.GenerateAccessToken(principal.Claims);

            var accessCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = DateTime.UtcNow.AddMinutes(15)
            };
            if (!HttpContext.Request.IsHttps)
                accessCookieOptions.Secure = false;
            Response.Cookies.Append("jwt_access_token", newAccessToken, accessCookieOptions);

            return Ok(new { message = "Token rafraîchi." });
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            var matricule = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new { matricule, name, email, role });
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt_access_token");
            Response.Cookies.Delete("jwt_refresh_token");
            return Ok(new { message = "Déconnexion réussie." });
        }

        [HttpGet("token")]
        [Authorize]
        public IActionResult GetToken()
        {
            var token = Request.Cookies["jwt_access_token"];
            if (string.IsNullOrEmpty(token))
                return Unauthorized();
            return Ok(new { token });
        }
    }
}
