using System.Threading.Tasks;

namespace Union.Playwright.NUnit.Pages.Interfaces
{
    public interface IOverlay : IComponent
    {
        Task CloseAsync();
    }
}
