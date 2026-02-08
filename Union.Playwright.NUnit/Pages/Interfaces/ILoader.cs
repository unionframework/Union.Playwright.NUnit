using System.Threading.Tasks;

namespace Union.Playwright.NUnit.Pages.Interfaces
{
    public interface ILoader : IComponent
    {
        Task WaitWhileVisibleAsync();
    }
}
