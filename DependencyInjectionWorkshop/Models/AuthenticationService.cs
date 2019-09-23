using System;

namespace DependencyInjectionWorkshop.Models
{
    /// <summary>
    /// 驗證使用者身分
    /// </summary>
    public class AuthenticationService
    {
        private readonly IProfileDao _profileDao;
        private readonly IFailedCounter _failedCounter;
        private readonly ISlackAdapter _slackAdapter;
        private readonly ISha256Adapter _sha256Adapter;
        private readonly IOtpService _otpService;
        private readonly ILogger _logger;

        public AuthenticationService(IProfileDao profileDao,IFailedCounter failedCounter,ISlackAdapter slackAdapter,ISha256Adapter sha256Adapter,IOtpService otpService,ILogger logger)
        {
            _profileDao = profileDao;
            _failedCounter = failedCounter;
            _slackAdapter = slackAdapter;
            _sha256Adapter = sha256Adapter;
            _otpService = otpService;
            _logger = logger;
        }

        public AuthenticationService()
        {

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