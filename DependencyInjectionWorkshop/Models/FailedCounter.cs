using System;
using System.Net.Http;

namespace DependencyInjectionWorkshop.Models
{
    public interface IFailedCounter
    {
        bool GetAccountIsLock(string accountId);
        void ResetFailedCounter(string accountId);
        void AddFailedCounter(string accountId);
        int GetFailedCount(string accountId);
    }

    public class FailedCounter : IFailedCounter
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
}