using System.Threading.Tasks;

namespace UI.Services
{
    public interface IRagService
    {
        Task<string> GetAnswerAsync(string userQuery);
    }
}
