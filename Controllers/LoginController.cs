using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Pizza;
using System.Buffers.Text;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Pizza.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        SqlTools sqlTools;

        private readonly IConfiguration _config;
        public LoginController(IConfiguration config)
        {
            _config = config;
            sqlTools = new SqlTools(_config);
        }


        [AllowAnonymous]
        [HttpPost]
        public ActionResult Login([FromBody] UserLogin userLogin)
        {
            Token token = new Token();
            try
            {
                token = sqlTools.TryLogin(userLogin);

                if (token != null)
                {
                    CookieOptions cookie = new()
                    {
                        Expires = DateTimeOffset.Now.AddDays(15),
                        MaxAge = TimeSpan.FromDays(15)
                    };
                    Response.Cookies.Append("token", token.UserToken, cookie);

                    return Ok(token);
                }
                else
                {
                    return NotFound("{\"error\": \"Username or Password is Invalid.\"}");
                }
            }
            catch (Exception ex) 
            {
                sqlTools.Logamuffin("Login", "Error", "Error Logging in user " + userLogin.Email + ". Token: " + token, ex.Message);
                return NotFound("{\"error\": \"Error logging in.\"}");
            }
        }
        
    }
}