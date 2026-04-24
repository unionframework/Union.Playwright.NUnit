using Union.Playwright.NUnit.Components;

namespace Union.Playwright.NUnit.Tests.Integration.StackOverflow
{
    public class TagItem : ItemBase
    {
        public override string ItemXcss
        {
            get
            {
                var containerRoot = this.Container.RootXcss;
                var baseXpath = containerRoot.StartsWith("xpath=")
                    ? containerRoot["xpath=".Length..]
                    : containerRoot;
                return $"xpath={baseXpath}//a[contains(@class,'s-tag') and normalize-space()='{this.Id}']";
            }
        }

        public UnionElement Link => new UnionElement(this.ParentPage, this.RootXcss);

        public TagItem(IContainer container, string id)
            : base(container, id)
        {
        }
    }
}
