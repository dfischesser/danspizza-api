using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

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
        public Boolean Login(Login login)
        {
            SqlTools sqlTools = new SqlTools();
            SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();
            DataSet ds = new DataSet();

            Boolean success = false;

            if (login != null)
            {
                if (login.Password == "test")
                {
                    success = true;
                }
            }

            //using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
            //{
            //    connection.Open();
            //    using (SqlCommand command = new SqlCommand("dbo.GetTestData", connection))
            //    {
            //        using (SqlDataAdapter da = new SqlDataAdapter n
            //        {
            //            da.SelectCommand = command;
            //            da.Fill(ds);
            //        }
            //    }
            //}


            // TODO: Add Error Logging for Coupons
            //if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            //{
            //    foreach (DataRow dr in ds.Tables[0].Rows)
            //    {
            //        var coupon = new Coupon();
            //        if (dr.ToString() != null)
            //        {
            //            coupon.CouponID = Convert.ToInt32(dr["id"]);
            //            coupon.CouponText = string.IsNullOrEmpty(dr["test_data"].ToString()) ? "" : dr["test_data"].ToString();
            //        }
            //    }
            //}

            return success;
        }
    }
}