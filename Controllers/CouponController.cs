using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.VisualBasic;
using System.Data;
using System.Diagnostics;

namespace Pizza.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CouponController : ControllerBase
    {
        SqlTools sqlTools = new SqlTools();

        private Coupons coupons = new Coupons();
        private List<Coupon> cpList = new List<Coupon>();

        [HttpGet]
        [Route("Get")]
        public IActionResult GetCoupons()
        {
            try
            {
                string url = HttpContext.Request.GetEncodedUrl();
                Debug.WriteLine(url, "Request URL: ");

                SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();
                DataSet ds = new DataSet();

                using (SqlConnection connection = new(sqlBuilder.ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("dbo.GetCoupons", connection))
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
                            coupon.CouponText = string.IsNullOrEmpty(dr["coupon"].ToString()) ? "" : dr["coupon"].ToString();
                        }
                        cpList.Add(coupon);
                    }
                }
                coupons.CouponList = cpList;

                return Ok(coupons);
            }
            catch (Exception ex)
            {
                sqlTools.Logamuffin("GetCoupon", "Error", "Error Getting Coupon", error: ex.Message, clientIP: Request.HttpContext.Connection.RemoteIpAddress.ToString());
                return NotFound(coupons);
            }
        } 
    }
}