using SlackAPI;

namespace DependencyInjectionWorkshop.Models
{
    public interface ISlackAdapter
    {
        void Notify(string accountId);
    }

    public class SlackAdapter : ISlackAdapter
    {
        public void Notify(string accountId)
        {

            string message = $"{accountId}:驗證失敗";
            var slackClient = new SlackClient("my api token");
            slackClient.PostMessage(response1 => { },"my channel",message,"my bot name");
        }
    }
}