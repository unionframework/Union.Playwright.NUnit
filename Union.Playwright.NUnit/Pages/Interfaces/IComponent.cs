using System.Threading.Tasks;

namespace Union.Playwright.NUnit.Pages.Interfaces
{
    public interface IComponent
    {
        IUnionPage ParentPage { get; }

        string ComponentName { get; set; }

        string FrameScss { get; set; }

        Task<bool> IsVisibleAsync();
    }
}
