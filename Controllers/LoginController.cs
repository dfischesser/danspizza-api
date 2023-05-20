using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.RegularExpressions;

namespace Pizza.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {

        private readonly ILogger<LoginController> _logger;

        public LoginController(ILogger<LoginController> logger)
        {
            _logger = logger;
        }

        [HttpPost("Post")]
        public Account Login(Login login)
        {
            SqlTools sqlTools = new SqlTools();
            SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();

            Account account = new Account();
            string? password = "";

            using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("dbo.GetUserByEmail", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    SqlParameter param = new SqlParameter();
                    param.ParameterName = "@email";
                    param.Value = login.Email;
                    param.DbType = DbType.String;
                    command.Parameters.Add(param);
                    // TODO: Add Error Logging for Users
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            account.UserID = Convert.ToInt32(reader["id"]);
                            account.FirstName = string.IsNullOrEmpty(reader["first_name"].ToString()) ? "" : reader["first_name"].ToString();
                            account.LastName = string.IsNullOrEmpty(reader["last_name"].ToString()) ? "" : reader["last_name"].ToString();
                            account.Email = string.IsNullOrEmpty(reader["email"].ToString()) ? "" : reader["email"].ToString();
                            account.Phone = string.IsNullOrEmpty(reader["phone"].ToString()) ? "" : reader["phone"].ToString();
                            account.Address1 = string.IsNullOrEmpty(reader["address1"].ToString()) ? "" : reader["address1"].ToString();
                            account.Address2 = string.IsNullOrEmpty(reader["address2"].ToString()) ? "" : reader["address2"].ToString();
                            account.City = string.IsNullOrEmpty(reader["city"].ToString()) ? "" : reader["city"].ToString();
                            account.State = Convert.ToInt32(reader["state"]);
                            account.Zip = string.IsNullOrEmpty(reader["zip"].ToString()) ? "" : reader["zip"].ToString();
                            password = string.IsNullOrEmpty(reader["password"].ToString()) ? "" : reader["password"].ToString();
                        }
                    }
                }
            }

            if (account.Email == login.Email && password == login.Password)
            {
                return account;
            }
            else
            {
                return new Account() { UserID = -1 };
            }

        }
    }
}