namespace Union.Playwright.NUnit.Components
{
    public abstract class ItemBase : ContainerBase
    {
        protected IContainer Container { get; }

        public string Id { get; }

        protected ItemBase(IContainer container, string id)
            : base(container.ParentPage)
        {
            this.Container = container;
            this.Id = id;
        }

        public abstract string ItemXcss { get; }

        public override string RootXcss => this.ItemXcss;
    }
}
