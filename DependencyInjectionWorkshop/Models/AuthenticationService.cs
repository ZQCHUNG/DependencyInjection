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
        public bool GetAccountIsLock(string accountId)
        {
            var isLockedResponse = new HttpClient() { BaseAddress = new Uri("http://joey.com/") }.PostAsJsonAsync("api/failedCounter/IsLocked",accountId).Result;

            isLockedResponse.EnsureSuccessStatusCode();
            var isLock = isLockedResponse.Content.ReadAsAsync<bool>().Result;
            return isLock;
        }

        public void ResetFailedCounter(string accountId)
        {
            var resetResponse = new HttpClient() { BaseAddress = new Uri("http://joey.com/") }.PostAsJsonAsync("api/failedCounter/Reset",accountId).Result;

            resetResponse.EnsureSuccessStatusCode();
        }

        public void AddFailedCounter(string accountId)
        {
            var addFailedCountResponse = new HttpClient() { BaseAddress = new Uri("http://joey.com/") }.PostAsJsonAsync("api/failedCounter/Add",accountId).Result;

            addFailedCountResponse.EnsureSuccessStatusCode();
        }

        public int GetFailedCount(string accountId)
        {
            var failedCountResponse =
                new HttpClient() { BaseAddress = new Uri("http://joey.com/") }.PostAsJsonAsync("api/failedCounter/GetFailedCount",accountId).Result;

            failedCountResponse.EnsureSuccessStatusCode();

            var failedCount = failedCountResponse.Content.ReadAsAsync<int>().Result;
            return failedCount;
        }
    }

    public class SlackAdapter
    {
        public void Notify(string accountId)
        {

            string message = $"{accountId}:驗證失敗";
            var slackClient = new SlackClient("my api token");
            slackClient.PostMessage(response1 => { },"my channel",message,"my bot name");
        }
    }

    public class Sha256Adapter
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

        public string GetCurrentOtp(string accountId)
        {
            var response = new HttpClient() { BaseAddress = new Uri("http://joey.com/") }.PostAsJsonAsync("api/otps",accountId).Result;
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

    public class Logger
    {
        public Logger()
        {
        }

        public void Info(string message)
        {
            var logger = NLog.LogManager.GetCurrentClassLogger();
            logger.Info(message);
        }
    }

    /// <summary>
    /// 驗證使用者身分
    /// </summary>
    public class AuthenticationService
    {
        private readonly ProfileDao _profileDao = new ProfileDao();
        private readonly FailedCounter _failedCounter = new FailedCounter();
        private readonly SlackAdapter _slackAdapter = new SlackAdapter();
        private readonly Sha256Adapter _sha256Adapter = new Sha256Adapter();
        private readonly OtpService _otpService;
        private readonly Logger _logger;

        public AuthenticationService()
        {
            _otpService = new OtpService();
            _logger = new Logger();
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

            var isLock = _failedCounter.GetAccountIsLock(accountId);
            if (isLock)
            {
                throw new FailedTooManyTimesException();
            }

            var passwordFromDb = _profileDao.GetPasswordFromDb(accountId);

            var hashedPassword = _sha256Adapter.GetHashedPassword(password);

            var currentOtp = _otpService.GetCurrentOtp(accountId);

            if (hashedPassword == passwordFromDb && otp == currentOtp)
            {
                _failedCounter.ResetFailedCounter(accountId);

                return true;
            }
            else
            {
                _failedCounter.AddFailedCounter(accountId);

                var failedCount = _failedCounter.GetFailedCount(accountId);
                _logger.Info($"accountId:{accountId} failed times:{failedCount}");

                _slackAdapter.Notify(accountId);

                return false;
            }
        }
    }

    public class FailedTooManyTimesException : Exception
    {
    }
}