using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Pizza.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {

        private readonly ILogger<AccountController> _logger;

        public AccountController(ILogger<AccountController> logger)
        {
            _logger = logger;
        }

        [HttpGet("Get")]
        public Account Get(int id)
        {
            SqlTools sqlTools = new SqlTools();
            SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();
            Account account = new Account();

            DataSet ds = new DataSet();
            using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("dbo.GetUserById", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    SqlParameter param = new SqlParameter();
                    param.ParameterName = "@id";
                    param.Value = id;
                    param.DbType = DbType.Int32;
                    command.Parameters.Add(param);
                    // TODO: Add Error Logging for Users
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (Convert.ToInt32(reader["id"]) == id)
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
                            }
                        }
                    }
                }
            }
            return account;
        }
    }
}