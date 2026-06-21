using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Playwright;
using NSubstitute;
using NUnit.Framework;
using Union.Playwright.NUnit.Attributes;
using Union.Playwright.NUnit.Components;
using Union.Playwright.NUnit.Pages.Interfaces;

namespace Union.Playwright.NUnit.Tests.Components
{
    public class TestListItem : ItemBase
    {
        public TestListItem(IContainer container, string id)
            : base(container, id) { }

        public override string ItemXcss => $".row[data-id='{this.Id}']";
    }

    public class TestList : ListBase<TestListItem>
    {
        public TestList(IUnionPage parentPage, string rootXcss = null)
            : base(parentPage, rootXcss) { }

        // ItemIdXcss is absolute: the row scope under the list root is built into the selector here
        // (same authoring pattern as ItemBase.ItemXcss), not concatenated by GetIdsAsync. No "xpath="
        // literal — GetLocatorFor routes the leading-"/" raw XPath to Playwright.
        public override string ItemIdXcss => this.InnerXcss(".row").XPath;

        public override string IdAttribute => "data-id";
    }

    public class InnerComponent : ComponentBase
    {
        public InnerComponent(IUnionPage parentPage, string rootXcss)
            : base(parentPage, rootXcss) { }
    }

    public class TestListItemWithInit : ItemBase
    {
        public TestListItemWithInit(IContainer container, string id)
            : base(container, id) { }

        public override string ItemXcss => $".row[data-id='{this.Id}']";

        [UnionInit("root:.inner")]
        public InnerComponent Inner;
    }

    public class TestListWithInit : ListBase<TestListItemWithInit>
    {
        public TestListWithInit(IUnionPage parentPage, string rootXcss = null)
            : base(parentPage, rootXcss) { }

        public override string ItemIdXcss => this.InnerXcss(".row").XPath;

        public override string IdAttribute => "data-id";
    }

    public class TestListTextContent : ListBase<TestListItem>
    {
        public TestListTextContent(IUnionPage parentPage, string rootXcss = null)
            : base(parentPage, rootXcss) { }

        public override string ItemIdXcss => this.InnerXcss(".row").XPath;
    }

    public class TestListPreScopedXpath : ListBase<TestListItem>
    {
        public TestListPreScopedXpath(IUnionPage parentPage, string rootXcss = null)
            : base(parentPage, rootXcss) { }

        // Already-scoped, absolute selector — must be used verbatim, not re-scoped under the root.
        public override string ItemIdXcss => "xpath=//table[@role='grid']//tr[@data-id]";

        public override string IdAttribute => "data-id";
    }

    public class TestListBareRelative : ListBase<TestListItem>
    {
        public TestListBareRelative(IUnionPage parentPage, string rootXcss = null)
            : base(parentPage, rootXcss) { }

        // A bare relative XCSS — resolved document-wide via Xcss.Parse; the list root is NOT applied.
        public override string ItemIdXcss => ".row";

        public override string IdAttribute => "data-id";
    }

    [TestFixture]
    public class ListBaseTests
    {
        private IUnionPage _mockPage;
        private IPage _mockPlaywrightPage;

        [SetUp]
        public void SetUp()
        {
            _mockPage = Substitute.For<IUnionPage>();
            _mockPlaywrightPage = Substitute.For<IPage>();
            _mockPage.PlaywrightPage.Returns(_mockPlaywrightPage);
        }

        [Test]
        public void CreateItem_CreatesItemWithCorrectId()
        {
            var list = new TestList(_mockPage, ".list");

            var item = list.CreateItem("item-1");

            item.Should().NotBeNull();
            item.Id.Should().Be("item-1");
        }

        [Test]
        public void CreateItem_ItemHasCorrectParentPage()
        {
            var list = new TestList(_mockPage, ".list");

            var item = list.CreateItem("item-1");

            item.ParentPage.Should().BeSameAs(_mockPage);
        }

        [Test]
        public void IdAttribute_ReturnsConfiguredValue()
        {
            var list = new TestList(_mockPage);

            list.IdAttribute.Should().Be("data-id");
        }

        [Test]
        public void IdAttribute_DefaultsToNull()
        {
            var list = new TestListTextContent(_mockPage);

            list.IdAttribute.Should().BeNull();
        }

        [Test]
        public async Task GetIdsAsync_WithAttribute_ReturnsAttributeValues()
        {
            var mockLocator = Substitute.For<ILocator>();
            mockLocator.CountAsync().Returns(2);

            var mockElement0 = Substitute.For<ILocator>();
            mockElement0
                .GetAttributeAsync("data-id", Arg.Any<LocatorGetAttributeOptions>())
                .Returns("id-a");
            var mockElement1 = Substitute.For<ILocator>();
            mockElement1
                .GetAttributeAsync("data-id", Arg.Any<LocatorGetAttributeOptions>())
                .Returns("id-b");

            mockLocator.Nth(0).Returns(mockElement0);
            mockLocator.Nth(1).Returns(mockElement1);

            _mockPlaywrightPage
                .Locator(Arg.Any<string>(), Arg.Any<PageLocatorOptions>())
                .Returns(mockLocator);

            var list = new TestList(_mockPage, ".list");
            var ids = await list.GetIdsAsync();

            ids.Should().BeEquivalentTo(new[] { "id-a", "id-b" });
        }

        [Test]
        public async Task GetIdsAsync_WithNullAttribute_UsesTextContent()
        {
            var mockLocator = Substitute.For<ILocator>();
            mockLocator.CountAsync().Returns(1);

            var mockElement0 = Substitute.For<ILocator>();
            mockElement0.TextContentAsync(Arg.Any<LocatorTextContentOptions>()).Returns("text-val");

            mockLocator.Nth(0).Returns(mockElement0);

            _mockPlaywrightPage
                .Locator(Arg.Any<string>(), Arg.Any<PageLocatorOptions>())
                .Returns(mockLocator);

            var list = new TestListTextContent(_mockPage, ".list");
            var ids = await list.GetIdsAsync();

            ids.Should().BeEquivalentTo(new[] { "text-val" });
        }

        [Test]
        public async Task GetIdsAsync_WhenItemIdXcssBuildsScopeViaInnerXcss_KeepsRootScope()
        {
            string capturedSelector = null;
            var mockLocator = Substitute.For<ILocator>();
            mockLocator.CountAsync().Returns(0);
            _mockPlaywrightPage
                .Locator(Arg.Do<string>(s => capturedSelector = s), Arg.Any<PageLocatorOptions>())
                .Returns(mockLocator);

            // TestList.ItemIdXcss is "xpath=" + InnerXcss(".row").XPath — the scope is authored in.
            var list = new TestList(_mockPage, ".list");
            await list.GetIdsAsync();

            capturedSelector.Should().StartWith("xpath=");
            capturedSelector.Should().Contain("list");
            capturedSelector.Should().Contain("row");
        }

        [Test]
        public async Task GetIdsAsync_WithPreScopedXpathItemIdXcss_UsesSelectorVerbatim()
        {
            string capturedSelector = null;
            var mockLocator = Substitute.For<ILocator>();
            mockLocator.CountAsync().Returns(0);
            _mockPlaywrightPage
                .Locator(Arg.Do<string>(s => capturedSelector = s), Arg.Any<PageLocatorOptions>())
                .Returns(mockLocator);

            var list = new TestListPreScopedXpath(_mockPage, ".list");
            await list.GetIdsAsync();

            // The absolute selector is passed through unchanged — the list root is NOT re-applied.
            capturedSelector.Should().Be("xpath=//table[@role='grid']//tr[@data-id]");
            capturedSelector.Should().NotContain("list");
        }

        [Test]
        public async Task GetIdsAsync_WithBareRelativeItemIdXcss_DoesNotConcatRoot()
        {
            string capturedSelector = null;
            var mockLocator = Substitute.For<ILocator>();
            mockLocator.CountAsync().Returns(0);
            _mockPlaywrightPage
                .Locator(Arg.Do<string>(s => capturedSelector = s), Arg.Any<PageLocatorOptions>())
                .Returns(mockLocator);

            var list = new TestListBareRelative(_mockPage, ".list");
            await list.GetIdsAsync();

            // A bare relative selector is resolved on its own (document-wide); the root is NOT prepended.
            capturedSelector.Should().StartWith("xpath=");
            capturedSelector.Should().Contain("row");
            capturedSelector.Should().NotContain("list");
        }

        [Test]
        public async Task GetItemsAsync_ReturnsItemsForEachId()
        {
            var mockLocator = Substitute.For<ILocator>();
            mockLocator.CountAsync().Returns(2);

            var mockElement0 = Substitute.For<ILocator>();
            mockElement0
                .GetAttributeAsync("data-id", Arg.Any<LocatorGetAttributeOptions>())
                .Returns("id-1");
            var mockElement1 = Substitute.For<ILocator>();
            mockElement1
                .GetAttributeAsync("data-id", Arg.Any<LocatorGetAttributeOptions>())
                .Returns("id-2");

            mockLocator.Nth(0).Returns(mockElement0);
            mockLocator.Nth(1).Returns(mockElement1);

            _mockPlaywrightPage
                .Locator(Arg.Any<string>(), Arg.Any<PageLocatorOptions>())
                .Returns(mockLocator);

            var list = new TestList(_mockPage, ".list");
            var items = await list.GetItemsAsync();

            items.Should().HaveCount(2);
            items[0].Id.Should().Be("id-1");
            items[1].Id.Should().Be("id-2");
        }

        [Test]
        public async Task FindSingleAsync_ReturnsFirstItem()
        {
            var mockLocator = Substitute.For<ILocator>();
            mockLocator.CountAsync().Returns(2);

            var mockElement0 = Substitute.For<ILocator>();
            mockElement0
                .GetAttributeAsync("data-id", Arg.Any<LocatorGetAttributeOptions>())
                .Returns("first");
            var mockElement1 = Substitute.For<ILocator>();
            mockElement1
                .GetAttributeAsync("data-id", Arg.Any<LocatorGetAttributeOptions>())
                .Returns("second");

            mockLocator.Nth(0).Returns(mockElement0);
            mockLocator.Nth(1).Returns(mockElement1);

            _mockPlaywrightPage
                .Locator(Arg.Any<string>(), Arg.Any<PageLocatorOptions>())
                .Returns(mockLocator);

            var list = new TestList(_mockPage, ".list");
            var item = await list.FindSingleAsync();

            item.Should().NotBeNull();
            item!.Id.Should().Be("first");
        }

        [Test]
        public async Task FindSingleAsync_WhenEmpty_ReturnsNull()
        {
            var mockLocator = Substitute.For<ILocator>();
            mockLocator.CountAsync().Returns(0);

            _mockPlaywrightPage
                .Locator(Arg.Any<string>(), Arg.Any<PageLocatorOptions>())
                .Returns(mockLocator);

            var list = new TestList(_mockPage, ".list");
            var item = await list.FindSingleAsync();

            item.Should().BeNull();
        }

        [Test]
        public async Task FindRandomAsync_WhenEmpty_ReturnsNull()
        {
            var mockLocator = Substitute.For<ILocator>();
            mockLocator.CountAsync().Returns(0);

            _mockPlaywrightPage
                .Locator(Arg.Any<string>(), Arg.Any<PageLocatorOptions>())
                .Returns(mockLocator);

            var list = new TestList(_mockPage, ".list");
            var item = await list.FindRandomAsync();

            item.Should().BeNull();
        }

        [Test]
        public void CreateItem_AutoInitializesUnionInitFields()
        {
            var list = new TestListWithInit(_mockPage, ".list");

            var item = list.CreateItem("item-1");

            item.Inner.Should().NotBeNull();
        }

        [Test]
        public void CreateItem_UnionInitField_HasCorrectParentPage()
        {
            var list = new TestListWithInit(_mockPage, ".list");

            var item = list.CreateItem("item-1");

            item.Inner.ParentPage.Should().BeSameAs(_mockPage);
        }

        [Test]
        public void CreateItem_UnionInitField_RootPrefixResolvesRelativeToItem()
        {
            var list = new TestListWithInit(_mockPage, ".list");

            var item = list.CreateItem("item-1");

            item.Inner.RootXcss.Should().StartWith("xpath=");
            item.Inner.RootXcss.Should().Contain("inner");
            item.Inner.RootXcss.Should().Contain("item-1");
        }

        [Test]
        public void CreateItem_UnionInitField_RegistersComponentOnPage()
        {
            var list = new TestListWithInit(_mockPage, ".list");

            list.CreateItem("item-1");

            _mockPage.Received().RegisterComponent(Arg.Any<InnerComponent>());
        }

        [Test]
        public async Task GetItemsAsync_AutoInitializesUnionInitFields()
        {
            var mockLocator = Substitute.For<ILocator>();
            mockLocator.CountAsync().Returns(2);

            var mockElement0 = Substitute.For<ILocator>();
            mockElement0
                .GetAttributeAsync("data-id", Arg.Any<LocatorGetAttributeOptions>())
                .Returns("id-1");
            var mockElement1 = Substitute.For<ILocator>();
            mockElement1
                .GetAttributeAsync("data-id", Arg.Any<LocatorGetAttributeOptions>())
                .Returns("id-2");

            mockLocator.Nth(0).Returns(mockElement0);
            mockLocator.Nth(1).Returns(mockElement1);

            _mockPlaywrightPage
                .Locator(Arg.Any<string>(), Arg.Any<PageLocatorOptions>())
                .Returns(mockLocator);

            var list = new TestListWithInit(_mockPage, ".list");
            var items = await list.GetItemsAsync();

            items[0].Inner.Should().NotBeNull();
            items[1].Inner.Should().NotBeNull();
        }

        [Test]
        public async Task FindSingleAsync_AutoInitializesUnionInitFields()
        {
            var mockLocator = Substitute.For<ILocator>();
            mockLocator.CountAsync().Returns(1);

            var mockElement0 = Substitute.For<ILocator>();
            mockElement0
                .GetAttributeAsync("data-id", Arg.Any<LocatorGetAttributeOptions>())
                .Returns("first");

            mockLocator.Nth(0).Returns(mockElement0);

            _mockPlaywrightPage
                .Locator(Arg.Any<string>(), Arg.Any<PageLocatorOptions>())
                .Returns(mockLocator);

            var list = new TestListWithInit(_mockPage, ".list");
            var item = await list.FindSingleAsync();

            item!.Inner.Should().NotBeNull();
        }
    }
}
