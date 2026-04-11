using Union.Playwright.NUnit.Attributes;
using Union.Playwright.NUnit.Components;
using Union.Playwright.NUnit.Pages.Interfaces;

namespace Union.Playwright.NUnit.Tests.Integration.StackOverflow
{
    public class QuestionTagList : ContainerBase
    {
        [UnionInit("root:a.s-tag")]
        public UnionElement TagLink { get; set; }

        public QuestionTagList(IUnionPage parentPage, string rootXcss)
            : base(parentPage, rootXcss)
        {
        }

        public async Task<List<string>> GetTagNamesAsync()
        {
            var links = this.PlaywrightPage.Locator("xpath=" + this.InnerXcss("a.s-tag").XPath);
            var count = await links.CountAsync();
            var names = new List<string>();
            for (int i = 0; i < count; i++)
            {
                names.Add(await links.Nth(i).TextContentAsync());
            }

            return names;
        }
    }
}
