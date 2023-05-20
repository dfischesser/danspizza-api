using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace Pizza.Controllers
{
    [Route("api/[controller]")]
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

        [EnableCors("MyPolicy")]
        [HttpGet("Get")]
        public Coupons Get()
        {
            SqlTools sqlTools = new SqlTools();

            string url = HttpContext.Request.GetEncodedUrl();
            Debug.WriteLine(url, "Request URL: ");

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