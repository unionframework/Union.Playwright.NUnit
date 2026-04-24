using Union.Playwright.NUnit.Components;
using Union.Playwright.NUnit.Pages.Interfaces;

namespace Union.Playwright.NUnit.Tests.Integration.StackOverflow
{
    public class QuestionList : ListBase<QuestionItem>
    {
        public override string ItemIdXcss => "div.s-post-summary";

        public override string IdAttribute => "data-post-id";

        public QuestionList(IUnionPage parentPage, string rootXcss)
            : base(parentPage, rootXcss)
        {
        }
    }
}
