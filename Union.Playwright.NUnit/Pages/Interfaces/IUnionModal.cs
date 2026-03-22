using System.Threading.Tasks;

namespace Union.Playwright.NUnit.Pages.Interfaces
{
    public interface IUnionModal : IComponent
    {
        Task DismissAsync();
        Task AcceptAsync();
    }
}
