using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using Dapper;
using SlackAPI;

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
        public bool Verify(string accountId, string password, string otp)
        {
            var httpClient = new HttpClient() { BaseAddress = new Uri("http://joey.com/") };

            var isLock = GetAccountIsLock(accountId,httpClient);
            if (isLock)
            {
                throw new FailedTooManyTimesException();
            }

            var passwordFromDb = GetPasswordFromDb(accountId);

            var hashedPassword = GetHashedPassword(password);

            var currentOtp = GetCurrentOtp(accountId,httpClient);

            if (hashedPassword == passwordFromDb && otp == currentOtp)
            {
                ResetFailedCounter(accountId,httpClient);

                return true;
            }
            else
            {
                AddFailedCounter(accountId,httpClient);

                LogFailedCounter(accountId,httpClient);

                Notify(accountId);

                return false;
            }
        }

        private static void Notify(string accountId)
        {

            string message = $"{accountId}:驗證失敗";
            var slackClient = new SlackClient("my api token");
            slackClient.PostMessage(response1 => { },"my channel",message,"my bot name");
        }

        private static void LogFailedCounter(string accountId,HttpClient httpClient)
        {

            var failedCount = GetFailedCount(accountId,httpClient);
            var logger = NLog.LogManager.GetCurrentClassLogger();
            logger.Info($"accountId:{accountId} failed times:{failedCount}");
        }

        private static int GetFailedCount(string accountId,HttpClient httpClient)
        {

            var failedCountResponse =
                httpClient.PostAsJsonAsync("api/failedCounter/GetFailedCount",accountId).Result;

            failedCountResponse.EnsureSuccessStatusCode();

            var failedCount = failedCountResponse.Content.ReadAsAsync<int>().Result;
            return failedCount;
        }

        private static void AddFailedCounter(string accountId,HttpClient httpClient)
        {

            var addFailedCountResponse = httpClient.PostAsJsonAsync("api/failedCounter/Add",accountId).Result;

            addFailedCountResponse.EnsureSuccessStatusCode();
        }

        private static void ResetFailedCounter(string accountId,HttpClient httpClient)
        {

            var resetResponse = httpClient.PostAsJsonAsync("api/failedCounter/Reset",accountId).Result;

            resetResponse.EnsureSuccessStatusCode();
        }

        private static string GetCurrentOtp(string accountId,HttpClient httpClient)
        {

            var response = httpClient.PostAsJsonAsync("api/otps",accountId).Result;
            if (response.IsSuccessStatusCode)
            {
            }
            else
            {
                throw new Exception($"web api error, accountId:{accountId}");
            }
            var currentOtp = response.Content.ReadAsAsync<string>().Result;
            return currentOtp;
        }

        private static string GetHashedPassword(string password)
        {

            var crypt = new System.Security.Cryptography.SHA256Managed();
            var hash = new StringBuilder();
            var crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(password));
            foreach (var theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }
            var hashedPassword = hash.ToString();
            return hashedPassword;
        }

        private static string GetPasswordFromDb(string accountId)
        {

            string passwordFromDb;
            using (var connection = new SqlConnection("my connection string"))
            {
                passwordFromDb = connection.Query<string>("spGetUserPassword",new {Id = accountId},
                    commandType: CommandType.StoredProcedure).SingleOrDefault();
            }
            return passwordFromDb;
        }

        private static bool GetAccountIsLock(string accountId,HttpClient httpClient)
        {

            var isLockedResponse = httpClient.PostAsJsonAsync("api/failedCounter/IsLocked",accountId).Result;

            isLockedResponse.EnsureSuccessStatusCode();
            var isLock = isLockedResponse.Content.ReadAsAsync<bool>().Result;
            return isLock;
        }
    }

    public class FailedTooManyTimesException : Exception
    {
    }
}