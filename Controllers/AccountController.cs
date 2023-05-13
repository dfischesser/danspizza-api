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
        public Account Get()
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
                    using (SqlDataAdapter da = new SqlDataAdapter())
                    {
                        da.SelectCommand = command;
                        da.Fill(ds);
                    }
                }
            }
            // TODO: Add Error Logging for Users
            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    if (dr.ToString() != null)
                    {
                        account.UserID = Convert.ToInt32(dr["id"]);
                        account.FirstName = string.IsNullOrEmpty(dr["first_name"].ToString()) ? "" : dr["first_name"].ToString();
                        account.LastName = string.IsNullOrEmpty(dr["last_name"].ToString()) ? "" : dr["last_name"].ToString();
                        account.Email= string.IsNullOrEmpty(dr["email"].ToString()) ? "" : dr["email"].ToString();
                        account.Phone = string.IsNullOrEmpty(dr["phone"].ToString()) ? "" : dr["phone"].ToString();
                        account.Address1 = string.IsNullOrEmpty(dr["address1"].ToString()) ? "" : dr["address1"].ToString();
                        account.Address2 = string.IsNullOrEmpty(dr["address2"].ToString()) ? "" : dr["address2"].ToString();
                        account.City = string.IsNullOrEmpty(dr["city"].ToString()) ? "" : dr["city"].ToString();
                        account.State = Convert.ToInt32(dr["state"]);
                        account.Zip = string.IsNullOrEmpty(dr["zip"].ToString()) ? "" : dr["zip"].ToString();
                    }
                }
            }
            return account;
        }
    }
}