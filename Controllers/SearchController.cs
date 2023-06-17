using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Pizza.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Search : ControllerBase
    {
        SqlTools sqlTools = new SqlTools();

        [HttpGet()]
        [Route("Address")]
        public async Task<string> SearchAddress(string address)
        {
            try
            {
                HttpResponseMessage response = new HttpResponseMessage();
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("https://atlas.microsoft.com/search/address/json?api-version=1.0&subscription-key=nbaeKPTLIg1Yme-hCVAyw0DToUeuljC811pMgXlqbVY&query=" + address),
                };
                using (HttpClient client = new HttpClient())
                {
                    response = await client.SendAsync(request).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                }

                return response.Content.ReadAsStringAsync().Result;
                //HttpResponseMessage response;
                //using (HttpClient client = new HttpClient())
                    //{
                    //    response = await client.GetAsync("https://atlas.microsoft.com/search/address/json?api-version=1.0&subscription-key=nbaeKPTLIg1Yme-hCVAyw0DToUeuljC811pMgXlqbVY&query=" + address);
                    //}
                    //return Ok(newAddress);
                    //return response.Content.ToString();
            }
            catch (Exception ex)
            { 
                sqlTools.Logamuffin("SearchAddress", "Error", "Error Searching Address", ex.Message);
                return "Request Failed";
                //return NotFound("Search Failed");
            }
        }

        //static async Task<string> GetAsync(HttpClient httpClient, string address)
        //{
        //    var requestURL = "search/address/json?api-version=1.0&subscription-key=nbaeKPTLIg1Yme-hCVAyw0DToUeuljC811pMgXlqbVY&query=" + address;
        //    using HttpResponseMessage response = await httpClient.GetAsync(requestURL);

        //    response.EnsureSuccessStatusCode();
        //    var responseString = await response.Content.ReadAsStringAsync();
        //    return responseString;
        //}
    }
}