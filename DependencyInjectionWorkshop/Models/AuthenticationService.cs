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
    public class ProfileDao
    {
        public string GetPasswordFromDb(string accountId)
        {

            string passwordFromDb;
            using (var connection = new SqlConnection("my connection string"))
            {
                passwordFromDb = connection.Query<string>("spGetUserPassword",new {Id = accountId},
                    commandType: CommandType.StoredProcedure).SingleOrDefault();
            }
            return passwordFromDb;
        }
    }

    public class FailedCounter
    {
        public bool GetAccountIsLock(string accountId,HttpClient httpClient)
        {

            var isLockedResponse = httpClient.PostAsJsonAsync("api/failedCounter/IsLocked",accountId).Result;

            isLockedResponse.EnsureSuccessStatusCode();
            var isLock = isLockedResponse.Content.ReadAsAsync<bool>().Result;
            return isLock;
        }

        public void ResetFailedCounter(string accountId,HttpClient httpClient)
        {
            var resetResponse = httpClient.PostAsJsonAsync("api/failedCounter/Reset",accountId).Result;

            resetResponse.EnsureSuccessStatusCode();
        }

        public void AddFailedCounter(string accountId,HttpClient httpClient)
        {
            var addFailedCountResponse = httpClient.PostAsJsonAsync("api/failedCounter/Add",accountId).Result;

            addFailedCountResponse.EnsureSuccessStatusCode();
        }

        public int GetFailedCount(string accountId,HttpClient httpClient)
        {
            var failedCountResponse =
                httpClient.PostAsJsonAsync("api/failedCounter/GetFailedCount",accountId).Result;

            failedCountResponse.EnsureSuccessStatusCode();

            var failedCount = failedCountResponse.Content.ReadAsAsync<int>().Result;
            return failedCount;
        }
    }

    public class slackAdapter
    {
        public void Notify(string accountId)
        {

            string message = $"{accountId}:驗證失敗";
            var slackClient = new SlackClient("my api token");
            slackClient.PostMessage(response1 => { },"my channel",message,"my bot name");
        }
    }

    public class _sha256Adapter
    {
        public string GetHashedPassword(string password)
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
    }

    public class OtpService
    {
        public OtpService()
        {
        }

        public string GetCurrentOtp(string accountId,HttpClient httpClient)
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
    }

    /// <summary>
    /// 驗證使用者身分
    /// </summary>
    public class AuthenticationService
    {
        private readonly ProfileDao _profileDao = new ProfileDao();
        private readonly FailedCounter _failedCounter = new FailedCounter();
        private readonly slackAdapter _slackAdapter = new slackAdapter();
        private readonly _sha256Adapter _sha256Adapter = new _sha256Adapter();
        private readonly OtpService _otpService;

        public AuthenticationService()
        {
            _otpService = new OtpService();
        }

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

            var isLock = _failedCounter.GetAccountIsLock(accountId,httpClient);
            if (isLock)
            {
                throw new FailedTooManyTimesException();
            }

            var passwordFromDb = _profileDao.GetPasswordFromDb(accountId);

            var hashedPassword = _sha256Adapter.GetHashedPassword(password);

            var currentOtp = _otpService.GetCurrentOtp(accountId,httpClient);

            if (hashedPassword == passwordFromDb && otp == currentOtp)
            {
                _failedCounter.ResetFailedCounter(accountId,httpClient);

                return true;
            }
            else
            {
                _failedCounter.AddFailedCounter(accountId,httpClient);

                LogFailedCounter(accountId,httpClient);

                _slackAdapter.Notify(accountId);

                return false;
            }
        }

        private void LogFailedCounter(string accountId,HttpClient httpClient)
        {
            var failedCount = _failedCounter.GetFailedCount(accountId,httpClient);
            LogMessage($"accountId:{accountId} failed times:{failedCount}");
        }

        private static void LogMessage(string message)
        {
            var logger = NLog.LogManager.GetCurrentClassLogger();
            logger.Info(message);
        }
    }

    public class FailedTooManyTimesException : Exception
    {
    }
}