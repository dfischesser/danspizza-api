using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Pizza;
using System;
using System.Buffers.Text;
using System.Data;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Web.Http.Results;

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
                var currentUser = sqlTools.GetCurrentUser(HttpContext.User);
                token = sqlTools.TryLogin(userLogin);

                if (token != null)
                {
                    CookieOptions cookieOptions = new()
                    {
                        Expires = DateTimeOffset.Now.AddDays(15),
                        MaxAge = TimeSpan.FromDays(15),
                        HttpOnly = true
                    };
                    var response = new HttpResponseMessage();

                    HttpContext.Response.Cookies.Append("token", token.UserToken, cookieOptions);

                    return Ok(new {message = "Login Success"});
                }
                else
                {
                    return NotFound(new {message = "Username or Password is Invalid."});
                }
            }
            catch (Exception ex) 
            {
                sqlTools.Logamuffin("Login", "Error", "Error Logging in user " + userLogin.Email + ". Token: " + token, error: ex.Message, clientIP: Request.HttpContext.Connection.RemoteIpAddress.ToString());
                return NotFound("{\"error\": \"Error logging in.\"}");
            }
        }
        
    }
}