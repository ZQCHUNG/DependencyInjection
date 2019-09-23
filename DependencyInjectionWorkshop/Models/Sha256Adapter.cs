using System.Text;

namespace DependencyInjectionWorkshop.Models
{
    public interface ISha256Adapter
    {
        string GetHashedPassword(string password);
    }

    public class Sha256Adapter : ISha256Adapter
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
}