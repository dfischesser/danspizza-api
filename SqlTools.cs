using Azure;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Pizza
{
    public class SqlTools
    {
        private SqlConnectionStringBuilder sqlBuilder = new SqlConnectionStringBuilder();

        //this field gets initialized at Startup.cs
        public static string conStr;

        private readonly IConfiguration _config;

        public SqlTools(IConfiguration config)
        {
            _config = config;
        }
        public SqlTools()
        {
        }

        public static SqlConnection GetConnection()
        {
            try
            {
                SqlConnection connection = new SqlConnection(conStr);
                return connection;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public Token TryLogin(UserLogin userLogin)
        {

            Token token = new();
            var user = Authenticate(userLogin);
            if (user != null)
            {
                token.UserToken = GenerateToken(user);
                return token;
            }
            return null;
        }

        public Token Step2Token(UserModel userModel)
        {
            return new Token() { UserToken = GenerateToken(userModel) };
        }

        //new Claim("email", user.Email),
        //        new Claim("firstName", user.UserFirstName),
        //        new Claim("role", user.Role),
        //        new Claim("userID", user.UserID)

        private string GenerateToken(UserModel user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim("email", user.Email),
                new Claim("firstName", user.UserFirstName),
                new Claim("role", user.Role),
                new Claim("userID", user.UserID)

            };
            var token = new JwtSecurityToken(_config["Jwt:Issuer"],
                _config["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddDays(15),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private UserModel? Authenticate(UserLogin userLogin)
        {
            UserModel? userModel = null;
            try
            {
                SqlConnectionStringBuilder sqlBuilder = CreateConnectionString();

                using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
                {
                    connection.Open();
                    if (userLogin.Email != null)
                    {
                        using (SqlCommand command = new SqlCommand("dbo.GetUserByEmail", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            SqlParameter param = new SqlParameter();
                            param.ParameterName = "@email";
                            param.Value = userLogin.Email;
                            param.DbType = DbType.String;
                            command.Parameters.Add(param);
                            // TODO: Add Error Logging for Users
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    userModel = new UserModel()
                                    {
                                        Email = reader["email"].ToString(),
                                        Password = reader["password"].ToString(),
                                        Role = reader["role"].ToString(),
                                        UserID = reader["id"].ToString(),
                                        UserFirstName = reader["first_name"].ToString()
                                    };
                                }
                            }
                        }
                    }
                    else if (userLogin.Id != null)
                    {
                        using (SqlCommand command = new SqlCommand("dbo.GetUserById", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            SqlParameter param = new SqlParameter();
                            param.ParameterName = "@id";
                            param.Value = userLogin.Id;
                            param.DbType = DbType.Int32;
                            command.Parameters.Add(param);
                            // TODO: Add Error Logging for Users
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    userModel = new UserModel()
                                    {
                                        Email = reader["email"].ToString(),
                                        Password = reader["password"].ToString(),
                                        Role = reader["role"].ToString(),
                                        UserID = reader["id"].ToString(),
                                        UserFirstName = reader["first_name"].ToString()
                                    };
                                }
                            }
                        }
                    }
                    
                }
                bool IsValid = false;
                if (userModel != null)
                {
                    IsValid = BCrypt.Net.BCrypt.Verify(userLogin.Password, userModel.Password);
                }
                else
                {
                    IsValid = false;
                }

                if (IsValid)
                {
                    Logamuffin("Login", "System", "User Logged In: " + userModel.Email + ". Role: " + userModel.Role);
                    return userModel;
                }
                return null;
            }
            catch (Exception ex)
            {
                Logamuffin("Authenticate (GetUserByEmail)", "Error", "Error Authenticating user " + userLogin.Email, ex.Message);
                return userModel;
            }
        }

        public UserModel GetCurrentUser(ClaimsPrincipal ident)
        {
            if (ident.Identity is ClaimsIdentity identity)
            {
                var userClaims = identity.Claims;
                return new UserModel
                {
                    Email = userClaims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value,
                    Role = userClaims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value,
                    UserID = userClaims.FirstOrDefault(x => x.Type == "userID")?.Value,
                    UserFirstName = userClaims.FirstOrDefault(x => x.Type == "firstName")?.Value
                };
            };

            return null;
        }

        public SqlConnectionStringBuilder CreateConnectionString()
        {
            sqlBuilder.ConnectionString = conStr;

            return sqlBuilder;
        }

        public void Logamuffin(string calledby, string type, string message, string error = "")
        {
            SqlTools sqlTools = new SqlTools();
            SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();;

            DataSet ds = new DataSet();
            using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("dbo.AddLogEntry", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    SqlParameter param = new SqlParameter();
                    param.ParameterName = "@errortype";
                    param.Value = type;
                    param.DbType = DbType.String;
                    command.Parameters.Add(param);
                    param = new SqlParameter();
                    param.ParameterName = "@error";
                    param.Value = error;
                    param.DbType = DbType.String;
                    command.Parameters.Add(param);
                    param = new SqlParameter();
                    param.ParameterName = "@message";
                    param.Value = message;
                    param.DbType = DbType.String;
                    command.Parameters.Add(param);
                    param = new SqlParameter();
                    param.ParameterName = "@calledby";
                    param.Value = calledby;
                    param.DbType = DbType.String;
                    command.Parameters.Add(param);

                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
