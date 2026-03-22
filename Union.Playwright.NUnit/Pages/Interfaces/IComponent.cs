using System.Threading.Tasks;

namespace Union.Playwright.NUnit.Pages.Interfaces
{
    public interface IComponent
    {
        IUnionPage ParentPage { get; }

        string ComponentName { get; set; }

        string FrameXcss { get; set; }

        Task<bool> IsVisibleAsync();
    }
}
