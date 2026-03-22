using System;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Union.Playwright.NUnit.Attributes;
using Union.Playwright.NUnit.Components;
using Union.Playwright.NUnit.Pages;
using Union.Playwright.NUnit.Pages.Interfaces;

namespace Union.Playwright.NUnit.Tests.Pages
{
    [TestFixture]
    public class WebPageBuilderTests
    {
        #region Test Doubles

        // Legacy: PageBase → New: UnionPage
        private class TestPage : UnionPage
        {
            public override string AbsolutePath => "/test";
        }

        // Legacy: TestComponent(IPage page, string xpath) : ComponentBase(page)
        // New: TestComponent(IUnionPage page, string xpath) : ComponentBase(page, xpath)
        private class TestComponent : ComponentBase
        {
            public readonly string Xpath;

            public TestComponent(IUnionPage page, string xpath = null)
                : base(page, xpath)
            {
                this.Xpath = xpath;
            }
        }

        // Legacy: TwoArgComponent(IPage page, string arg1, string arg2) : ComponentBase(page)
        private class TwoArgComponent : ComponentBase
        {
            public readonly string Arg1;
            public readonly string Arg2;

            public TwoArgComponent(IUnionPage page, string arg1, string arg2)
                : base(page, arg1)
            {
                this.Arg1 = arg1;
                this.Arg2 = arg2;
            }
        }

        // Legacy: TestContainer : ContainerBase(IPage, string rootScss)
        private class TestContainer : ContainerBase
        {
            public TestContainer(IUnionPage parent, string rootScss = null)
                : base(parent, rootScss)
            {
            }
        }

        // Legacy: TestItem(IContainer, string id) : ItemBase — same
        private class TestItem : ItemBase
        {
            public TestItem(IContainer container, string id)
                : base(container, id)
            {
            }

            public override string ItemScss => Container.InnerScss($"//li[@id='{Id}']");
        }

        // Legacy: TestAlert : AlertBase(IPage) → New: TestModal : ComponentBase, IUnionModal
        private class TestModal : ComponentBase, IUnionModal
        {
            public TestModal(IUnionPage page)
                : base(page)
            {
            }

            public Task DismissAsync() => Task.CompletedTask;
            public Task AcceptAsync() => Task.CompletedTask;
        }

        // Legacy: TestProgressBar : ComponentBase, IProgressBar → New: TestLoader : ComponentBase, ILoader
        private class TestLoader : ComponentBase, ILoader
        {
            public TestLoader(IUnionPage page)
                : base(page)
            {
            }

            public Task WaitWhileVisibleAsync() => Task.CompletedTask;
        }

        #endregion

        #region Fixture Classes for Field/Property Initialization

        // Legacy: MixedContainer : ComponentBase (NOT IContainer)
        // New: plain class (NOT IContainer) — preserves legacy non-container behavior
        private class MixedContainer
        {
            [UnionInit("//div[@id='pub']")]
            public TestComponent PublicField;

            [UnionInit("//div[@id='priv']")]
            private TestComponent _privateField;

            [UnionInit("//div[@id='prop']")]
            public TestComponent PublicProperty { get; set; }

            public TestComponent NotAnnotated;

            public TestComponent GetPrivateField() => _privateField;
        }

        #endregion

        #region Fixture Classes for Nested Components

        // Legacy: Level1/2/3Container : ContainerBase — uses root: prefix
        private class Level3Container : ContainerBase
        {
            [UnionInit("root:span.leaf")]
            public TestComponent Leaf;

            public Level3Container(IUnionPage parent, string rootScss = null)
                : base(parent, rootScss)
            {
            }
        }

        private class Level2Container : ContainerBase
        {
            [UnionInit("root:div.l3")]
            public Level3Container Level3;

            public Level2Container(IUnionPage parent, string rootScss = null)
                : base(parent, rootScss)
            {
            }
        }

        private class Level1Container : ContainerBase
        {
            [UnionInit("root:div.l2")]
            public Level2Container Level2;

            public Level1Container(IUnionPage parent, string rootScss = null)
                : base(parent, rootScss)
            {
            }
        }

        #endregion

        #region Fixture Classes for ComponentName

        // Legacy: CustomNameContainer : ComponentBase (NOT IContainer)
        private class CustomNameContainer
        {
            [UnionInit("//div", ComponentName = "Custom")]
            public TestComponent Named;

            [UnionInit("//div")]
            public TestComponent Unnamed;
        }

        #endregion

        #region Fixture Classes for Registration

        // Legacy: AlertContainer : ComponentBase — [WebComponent] with no args for TestAlert
        private class ModalContainer
        {
            [UnionInit]
            public TestModal Modal;
        }

        // Legacy: ProgressBarContainer : ComponentBase — [WebComponent] with no args for TestProgressBar
        private class LoaderContainer
        {
            [UnionInit]
            public TestLoader Loader;
        }

        // Legacy: RegularContainer : ComponentBase (NOT IContainer)
        private class RegularContainer
        {
            [UnionInit("//div")]
            public TestComponent Comp;
        }

        #endregion

        #region Fixture Classes for Selector - root: prefix

        // Legacy: uses root: prefix to mark args for concatenation
        private class RootPrefixSelectorContainer : ContainerBase
        {
            [UnionInit("root:div[@class='child']")]
            public TestComponent RootPrefixed;

            public RootPrefixSelectorContainer(IUnionPage parent, string rootScss = null)
                : base(parent, rootScss)
            {
            }
        }

        // Legacy: non-prefixed selector should pass through unchanged
        private class NonPrefixedSelectorContainer : ContainerBase
        {
            [UnionInit("//span[@id='absolute']")]
            public TestComponent NonPrefixed;

            public NonPrefixedSelectorContainer(IUnionPage parent, string rootScss = null)
                : base(parent, rootScss)
            {
            }
        }

        // Legacy: PageWithRootPrefixChild : ComponentBase (NOT IContainer)
        // root: on non-container parent passes through unchanged
        private class NonContainerWithRootPrefix
        {
            [UnionInit("root:div")]
            public TestComponent Child;
        }

        #endregion

        #region Fixture Classes for Multi-Arg Selector

        // Legacy: MultiArgContainer : ContainerBase — root: on first arg only
        private class MultiArgContainer : ContainerBase
        {
            [UnionInit("root:div", "//span")]
            public TwoArgComponent Child;

            public MultiArgContainer(IUnionPage parent, string rootScss = null)
                : base(parent, rootScss)
            {
            }
        }

        #endregion

        #region Fixture Classes for Nested Container Selectors

        // Legacy: OuterContainer/InnerContainer : ContainerBase — root: prefix
        private class InnerContainer : ContainerBase
        {
            [UnionInit("root:span.leaf")]
            public TestComponent Leaf;

            public InnerContainer(IUnionPage parent, string rootScss = null)
                : base(parent, rootScss)
            {
            }
        }

        private class OuterContainer : ContainerBase
        {
            [UnionInit("root:div.inner")]
            public InnerContainer Inner;

            public OuterContainer(IUnionPage parent, string rootScss = null)
                : base(parent, rootScss)
            {
            }
        }

        #endregion

        #region Fixture Classes from WebPageBuilderTest.cs

        // Legacy: Container : ContainerBase — root: and non-prefixed side by side
        private class LegacySelectorContainer : ContainerBase
        {
            [UnionInit("root:div[text()='mytext']")]
            public TestComponent Component1;

            [UnionInit("//div[text()='mytext']")]
            public TestComponent Component2;

            public LegacySelectorContainer(IUnionPage parent, string rootScss = null)
                : base(parent, rootScss)
            {
            }
        }

        #endregion

        private TestPage _page;

        [SetUp]
        public void SetUp()
        {
            _page = new TestPage();
        }

        // ----------------------------------------------------------------
        // 1. Field and Property Initialization
        // ----------------------------------------------------------------

        [Test]
        public void InitComponents_InitializesBothFieldsAndProperties()
        {
            var container = new MixedContainer();

            WebPageBuilder.InitComponent(_page, container);

            // Public field initialized with correct xpath
            container.PublicField.Should().NotBeNull();
            container.PublicField.Xpath.Should().Be("//div[@id='pub']");
            container.PublicField.ParentPage.Should().BeSameAs(_page);

            // Private field initialized with correct xpath
            container.GetPrivateField().Should().NotBeNull();
            container.GetPrivateField().Xpath.Should().Be("//div[@id='priv']");
            container.GetPrivateField().ParentPage.Should().BeSameAs(_page);

            // Property initialized with correct xpath
            container.PublicProperty.Should().NotBeNull();
            container.PublicProperty.Xpath.Should().Be("//div[@id='prop']");
            container.PublicProperty.ParentPage.Should().BeSameAs(_page);

            // Unannotated field remains null
            container.NotAnnotated.Should().BeNull();
        }

        // ----------------------------------------------------------------
        // 2. Nested Component Initialization
        // ----------------------------------------------------------------

        [Test]
        public void InitComponents_NestedComponents_AllReferenceTheSamePage()
        {
            var level1 = new Level1Container(_page, "//*[@id='root']");

            WebPageBuilder.InitComponent(_page, level1);

            level1.Level2.Should().NotBeNull();
            level1.Level2.Level3.Should().NotBeNull();
            level1.Level2.Level3.Leaf.Should().NotBeNull();

            level1.ParentPage.Should().BeSameAs(_page);
            level1.Level2.ParentPage.Should().BeSameAs(_page);
            level1.Level2.Level3.ParentPage.Should().BeSameAs(_page);
            level1.Level2.Level3.Leaf.ParentPage.Should().BeSameAs(_page);
        }

        // ----------------------------------------------------------------
        // 3. ItemBase Initialization
        // ----------------------------------------------------------------

        [Test]
        public void CreateComponent_ItemBase_ReceivesContainerAsFirstArg()
        {
            var container = new TestContainer(_page, "//*[@id='list']");
            var attr = new UnionInit("item1");

            var item = (TestItem)WebPageBuilder.CreateComponent(_page, container, typeof(TestItem), attr);

            item.Should().NotBeNull();
        }

        [Test]
        public void CreateComponent_ItemBase_ParentPageComesFromContainer()
        {
            var container = new TestContainer(_page, "//*[@id='list']");
            var attr = new UnionInit("item1");

            var item = (TestItem)WebPageBuilder.CreateComponent(_page, container, typeof(TestItem), attr);

            item.ParentPage.Should().BeSameAs(_page);
            container.ParentPage.Should().BeSameAs(_page);
        }

        [Test]
        public void CreateItems_EachItemHasCorrectId()
        {
            var container = new TestContainer(_page, "//*[@id='list']");
            var ids = new[] { "a", "b", "c" };

            var items = WebPageBuilder.CreateItems<TestItem>(container, ids);

            items.Should().HaveCount(3);
            items[0].Id.Should().Be("a");
            items[1].Id.Should().Be("b");
            items[2].Id.Should().Be("c");
        }

        // ----------------------------------------------------------------
        // 4. Selector Concatenation
        // ----------------------------------------------------------------

        [Test]
        public void SelectorWithRootPrefix_IsConcatenatedWithParentSelector()
        {
            // Legacy: [WebComponent("root:div[@class='child']")] on ContainerBase
            // root: prefix stripped, remainder concatenated with parent selector
            var container = new RootPrefixSelectorContainer(_page, "//*[@id='root']");

            WebPageBuilder.InitComponent(_page, container);

            container.RootPrefixed.Xpath.Should().Be(
                "//*[@id='root']/descendant::div[@class='child']");
        }

        [Test]
        public void SelectorWithoutPrefix_PassedUnchanged()
        {
            // Legacy: [WebComponent("//span[@id='absolute']")] on ContainerBase
            // No root: prefix → selector passes through unchanged
            var container = new NonPrefixedSelectorContainer(_page, "//*[@id='root']");

            WebPageBuilder.InitComponent(_page, container);

            container.NonPrefixed.Xpath.Should().Be("//span[@id='absolute']");
        }

        [Test]
        public void NonContainerParent_DoesNotProcessRootPrefix()
        {
            // Legacy: [WebComponent("root:div")] on ComponentBase (NOT IContainer)
            // root: prefix passes through unchanged because parent is not a container
            var container = new NonContainerWithRootPrefix();

            WebPageBuilder.InitComponent(_page, container);

            container.Child.Xpath.Should().Be("root:div");
        }

        [Test]
        public void MultipleArgs_OnlyRootPrefixedArgsReplaced()
        {
            // Legacy: [WebComponent("root:div", "//span")] on ContainerBase
            // Only the root:-prefixed arg is concatenated; "//span" passes through unchanged
            var container = new MultiArgContainer(_page, "//*[@id='root']");

            WebPageBuilder.InitComponent(_page, container);

            container.Child.Should().NotBeNull();
            container.Child.Arg1.Should().Be("//*[@id='root']/descendant::div");
            container.Child.Arg2.Should().Be("//span");
        }

        [Test]
        public void NestedContainers_SelectorsConcatenateHierarchically()
        {
            // Legacy: root: selectors at each nesting level build up the full path
            var outer = new OuterContainer(_page, "//*[@id='outer']");

            WebPageBuilder.InitComponent(_page, outer);

            outer.Inner.Should().NotBeNull();
            outer.Inner.Leaf.Should().NotBeNull();

            var expectedInnerLeaf =
                "//*[@id='outer']/descendant::div[contains(@class,'inner')]/descendant::span[contains(@class,'leaf')]";
            outer.Inner.Leaf.Xpath.Should().Be(expectedInnerLeaf);
        }

        // ----------------------------------------------------------------
        // 5. ComponentName
        // ----------------------------------------------------------------

        [Test]
        public void ComponentName_WhenSetOnAttribute_UsesAttributeValue()
        {
            var container = new CustomNameContainer();

            WebPageBuilder.InitComponent(_page, container);

            container.Named.ComponentName.Should().Be("Custom");
        }

        [Test]
        public void ComponentName_WhenNotSet_DefaultsToMemberName()
        {
            var container = new CustomNameContainer();

            WebPageBuilder.InitComponent(_page, container);

            container.Unnamed.ComponentName.Should().Be("Unnamed");
        }

        // ----------------------------------------------------------------
        // 6. Component Registration
        // ----------------------------------------------------------------

        [Test]
        public void RegisterComponent_IUnionModal_AddedToPageModals()
        {
            // Legacy: [WebComponent] (no args) on TestAlert : AlertBase
            var container = new ModalContainer();

            WebPageBuilder.InitComponent(_page, container);

            _page.Modals.Should().HaveCount(1);
            _page.Modals[0].Should().BeSameAs(container.Modal);
        }

        [Test]
        public void RegisterComponent_ILoader_AddedToPageLoaders()
        {
            // Legacy: [WebComponent] (no args) on TestProgressBar : IProgressBar
            var container = new LoaderContainer();

            WebPageBuilder.InitComponent(_page, container);

            _page.Loaders.Should().HaveCount(1);
            _page.Loaders[0].Should().BeSameAs(container.Loader);
        }

        [Test]
        public void RegisterComponent_RegularComponent_NotInModalsOrLoaders()
        {
            var container = new RegularContainer();

            WebPageBuilder.InitComponent(_page, container);

            _page.Modals.Should().BeEmpty();
            _page.Loaders.Should().BeEmpty();
        }

        // ----------------------------------------------------------------
        // 7. Error Handling
        // ----------------------------------------------------------------

        [Test]
        public void InitComponents_NullPage_ThrowsArgumentNullException()
        {
            var act = () => WebPageBuilder.InitComponent(null, new TestPage());

            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("page");
        }

        [Test]
        public void InitComponents_NullContainer_DoesNotThrow()
        {
            // Legacy: passing null container defaults to page — does not throw
            var act = () => WebPageBuilder.InitComponent(_page, null);

            act.Should().NotThrow();
        }

        [Test]
        public void InitPage_NullPage_ThrowsArgumentNullException()
        {
            var act = () => WebPageBuilder.InitPage(null);

            act.Should().Throw<ArgumentNullException>();
        }

        // ----------------------------------------------------------------
        // 8. From WebPageBuilderTest.cs (legacy root: prefix tests)
        // ----------------------------------------------------------------

        [Test]
        public void DoNotAddRootWithoutPrefix()
        {
            // Legacy: non-prefixed selector on ContainerBase passes through unchanged
            var container = new LegacySelectorContainer(_page, "//*[@id='rootelementid']");

            WebPageBuilder.InitComponent(_page, container);

            container.Component2.Xpath.Should().Be("//div[text()='mytext']");
        }

        [Test]
        public void ReplacePrefixWithRootSelector()
        {
            // Legacy: root:-prefixed selector on ContainerBase is concatenated with parent
            var container = new LegacySelectorContainer(_page, "//*[@id='rootelementid']");

            WebPageBuilder.InitComponent(_page, container);

            container.Component1.Xpath.Should().Be(
                "//*[@id='rootelementid']/descendant::div[text()='mytext']");
        }
    }
}
