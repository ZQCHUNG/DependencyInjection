using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using Dapper;

namespace DependencyInjectionWorkshop.Models
{
    /// <summary>
    /// 驗證使用者身分
    /// </summary>
    public class AuthenticationService
    {
        /// <summary>
        /// Step 1 : 取得使用者帳號、密碼、otp
        /// 2 : 透過帳號去DB撈密碼
        /// 3 : 將收到的密碼做Hash
        /// 4.: 比對密碼及otp是否正確
        /// 5 : 若正確Return True ， 反之False
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="password"></param>
        /// <param name="otp"></param>
        /// <returns></returns>
        public bool Verify(string accountId,string password,string otp)
        {
            string passwordFromDb;
            using (var connection = new SqlConnection("my connection string"))
            {
                passwordFromDb = connection.Query<string>("spGetUserPassword",new {Id = accountId},
                    commandType: CommandType.StoredProcedure).SingleOrDefault();
            }

            var crypt = new System.Security.Cryptography.SHA256Managed();
            var hash = new StringBuilder();
            var crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(password));
            foreach (var theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }
            var hashedPassword = hash.ToString();

            var httpClient = new HttpClient() {BaseAddress = new Uri("http://joey.com/")};
            var response = httpClient.PostAsJsonAsync("api/otps",accountId).Result;
            if (response.IsSuccessStatusCode)
            {
            }
            else
            {
                throw new Exception($"web api error, accountId:{accountId}");
            }
            var currentOtp = response.Content.ReadAsAsync<string>().Result;

            if (hashedPassword == passwordFromDb && otp == currentOtp)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public string GetPassword(string accountId)
        {
            using (var connection = new SqlConnection("my connection string"))
            {
                var password = connection.Query<string>("spGetUserPassword",new {Id = accountId},
                    commandType: CommandType.StoredProcedure).SingleOrDefault();

                return password;
            }
        }

        public string GetHash(string plainText)
        {
            var crypt = new System.Security.Cryptography.SHA256Managed();
            var hash = new StringBuilder();
            var crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(plainText));
            foreach (var theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString();
        }

        public string GetOtp(string accountId)
        {
            var httpClient = new HttpClient() {BaseAddress = new Uri("http://joey.com/")};
            var response = httpClient.PostAsJsonAsync("api/otps",accountId).Result;
            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsAsync<string>().Result;
            }
            else
            {
                throw new Exception($"web api error, accountId:{accountId}");
            }
        }
    }
}