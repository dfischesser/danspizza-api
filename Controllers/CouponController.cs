using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Pizza.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CouponController : ControllerBase
    {

        private readonly ILogger<CouponController> _logger;

        private Coupons coupons = new Coupons();
        private List<Coupon> cpList = new List<Coupon>();

        public CouponController(ILogger<CouponController> logger)
        {
            _logger = logger;
        }

        [HttpGet("Get")]
        public Coupons Get()
        {
            SqlTools sqlTools = new SqlTools();
            SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();
            DataSet ds = new DataSet();

            using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("dbo.GetTestData", connection))
                {
                    using (SqlDataAdapter da = new SqlDataAdapter())
                    {
                        da.SelectCommand = command;
                        da.Fill(ds);
                    }
                }
            }


            // TODO: Add Error Logging for Coupons
            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    var coupon = new Coupon();
                    if (dr.ToString() != null)
                    {
                        coupon.CouponID = Convert.ToInt32(dr["id"]);
                        coupon.CouponText = string.IsNullOrEmpty(dr["test_data"].ToString()) ? "" : dr["test_data"].ToString();
                    }
                    cpList.Add(coupon);
                }
            }
            coupons.CouponList = cpList;

            return coupons;
        }
    }
}