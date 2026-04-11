using Union.Playwright.NUnit.Components;
using Union.Playwright.NUnit.Pages.Interfaces;

namespace Union.Playwright.NUnit.Tests.Integration.StackOverflow
{
    public class QuestionTagList : ListBase<TagItem>
    {
        public override string ItemIdXcss => "a.s-tag";

        public QuestionTagList(IUnionPage parentPage, string rootXcss)
            : base(parentPage, rootXcss)
        {
        }
    }
}
