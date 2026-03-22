using System;
using FluentAssertions;
using Microsoft.Playwright;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;
using Union.Playwright.NUnit.Components;
using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.Services;

namespace Union.Playwright.NUnit.Tests.Components
{
    public class TestComponent : ComponentBase
    {
        public TestComponent(IUnionPage parentPage, string rootScss = null)
            : base(parentPage, rootScss)
        {
        }
    }

    [TestFixture]
    public class ComponentBaseTests
    {
        private IUnionPage _mockPage;
        private IPage _mockPlaywrightPage;
        private IUnionService _mockService;
        private IBrowserAction _mockAction;

        [SetUp]
        public void SetUp()
        {
            _mockPage = Substitute.For<IUnionPage>();
            _mockPlaywrightPage = Substitute.For<IPage>();
            _mockService = Substitute.For<IUnionService>();
            _mockAction = Substitute.For<IBrowserAction>();
            _mockPage.PlaywrightPage.Returns(_mockPlaywrightPage);
            _mockPage.Service.Returns(_mockService);
            _mockService.Action.Returns(_mockAction);
        }

        [Test]
        public void Constructor_WhenParentPageIsNull_ThrowsArgumentNullException()
        {
            var act = () => new TestComponent(null);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("parentPage");
        }

        [Test]
        public void ParentPage_ReturnsProvidedPage()
        {
            var component = new TestComponent(_mockPage);

            component.ParentPage.Should().BeSameAs(_mockPage);
        }

        [Test]
        public void RootScss_WhenNoScssProvided_DefaultsToHtml()
        {
            var component = new TestComponent(_mockPage);

            component.RootScss.Should().Be("html");
        }

        [Test]
        public void RootScss_WhenScssProvided_ReturnsIt()
        {
            var component = new TestComponent(_mockPage, ".my-component");

            component.RootScss.Should().Be(".my-component");
        }

        [Test]
        public void ComponentName_CanBeSetAndRetrieved()
        {
            var component = new TestComponent(_mockPage);
            component.ComponentName = "My Component";

            component.ComponentName.Should().Be("My Component");
        }

        [Test]
        public void FrameXcss_CanBeSetAndRetrieved()
        {
            var component = new TestComponent(_mockPage);
            component.FrameXcss = "iframe.main";

            component.FrameXcss.Should().Be("iframe.main");
        }

        [Test]
        public async Task IsVisibleAsync_DelegatesToLocator()
        {
            var mockLocator = Substitute.For<ILocator>();
            mockLocator.IsVisibleAsync(Arg.Any<LocatorIsVisibleOptions>()).Returns(true);
            _mockPlaywrightPage.Locator("html", Arg.Any<PageLocatorOptions>()).Returns(mockLocator);

            var component = new TestComponent(_mockPage);
            var result = await component.IsVisibleAsync();

            result.Should().BeTrue();
        }
    }
}
