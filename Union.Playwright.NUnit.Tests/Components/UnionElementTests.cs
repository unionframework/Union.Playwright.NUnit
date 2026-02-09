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
    [TestFixture]
    public class UnionElementTests
    {
        private IUnionPage _mockPage;
        private IPage _mockPlaywrightPage;
        private ILocator _mockLocator;
        private IUnionService _mockService;
        private IBrowserAction _mockAction;

        [SetUp]
        public void SetUp()
        {
            _mockPage = Substitute.For<IUnionPage>();
            _mockPlaywrightPage = Substitute.For<IPage>();
            _mockLocator = Substitute.For<ILocator>();
            _mockService = Substitute.For<IUnionService>();
            _mockAction = Substitute.For<IBrowserAction>();
            _mockPage.PlaywrightPage.Returns(_mockPlaywrightPage);
            _mockPage.Service.Returns(_mockService);
            _mockService.Action.Returns(_mockAction);
            _mockPlaywrightPage.Locator(".element", Arg.Any<PageLocatorOptions>()).Returns(_mockLocator);
        }

        private UnionElement CreateElement() => new UnionElement(_mockPage, ".element");

        [Test]
        public void ImplementsILocator()
        {
            var element = CreateElement();

            element.Should().BeAssignableTo<ILocator>();
        }

        [Test]
        public void InheritsComponentBase()
        {
            var element = CreateElement();

            element.Should().BeAssignableTo<ComponentBase>();
        }

        [Test]
        public void RootScss_ReturnsProvidedSelector()
        {
            var element = CreateElement();

            element.RootScss.Should().Be(".element");
        }

        [Test]
        public async Task ClickAsync_DelegatesToRoot()
        {
            var element = CreateElement();

            await element.ClickAsync();

            await _mockLocator.Received(1).ClickAsync(Arg.Any<LocatorClickOptions>());
        }

        [Test]
        public async Task FillAsync_DelegatesToRoot()
        {
            var element = CreateElement();

            await element.FillAsync("test value");

            await _mockLocator.Received(1).FillAsync("test value", Arg.Any<LocatorFillOptions>());
        }

        [Test]
        public async Task TextContentAsync_DelegatesToRoot()
        {
            _mockLocator.TextContentAsync(Arg.Any<LocatorTextContentOptions>()).Returns("hello");
            var element = CreateElement();

            var result = await element.TextContentAsync();

            result.Should().Be("hello");
        }

        [Test]
        public async Task IsVisibleAsync_DelegatesToRoot()
        {
            _mockLocator.IsVisibleAsync(Arg.Any<LocatorIsVisibleOptions>()).Returns(true);
            var element = CreateElement();

            var result = await ((ILocator)element).IsVisibleAsync();

            result.Should().BeTrue();
        }

        [Test]
        public async Task CountAsync_DelegatesToRoot()
        {
            _mockLocator.CountAsync().Returns(5);
            var element = CreateElement();

            var result = await element.CountAsync();

            result.Should().Be(5);
        }

        [Test]
        public void First_DelegatesToRoot()
        {
            var firstLocator = Substitute.For<ILocator>();
            _mockLocator.First.Returns(firstLocator);
            var element = CreateElement();

            var result = element.First;

            result.Should().BeSameAs(firstLocator);
        }

        [Test]
        public void Last_DelegatesToRoot()
        {
            var lastLocator = Substitute.For<ILocator>();
            _mockLocator.Last.Returns(lastLocator);
            var element = CreateElement();

            var result = element.Last;

            result.Should().BeSameAs(lastLocator);
        }

        [Test]
        public void Locator_DelegatesToRoot()
        {
            var childLocator = Substitute.For<ILocator>();
            _mockLocator.Locator("span", Arg.Any<LocatorLocatorOptions>()).Returns(childLocator);
            var element = CreateElement();

            var result = element.Locator("span");

            result.Should().BeSameAs(childLocator);
        }

        [Test]
        public void Nth_DelegatesToRoot()
        {
            var nthLocator = Substitute.For<ILocator>();
            _mockLocator.Nth(2).Returns(nthLocator);
            var element = CreateElement();

            var result = element.Nth(2);

            result.Should().BeSameAs(nthLocator);
        }

        [Test]
        public async Task HoverAsync_DelegatesToRoot()
        {
            var element = CreateElement();

            await element.HoverAsync();

            await _mockLocator.Received(1).HoverAsync(Arg.Any<LocatorHoverOptions>());
        }

        [Test]
        public async Task GetAttributeAsync_DelegatesToRoot()
        {
            _mockLocator.GetAttributeAsync("href", Arg.Any<LocatorGetAttributeOptions>()).Returns("https://example.com");
            var element = CreateElement();

            var result = await element.GetAttributeAsync("href");

            result.Should().Be("https://example.com");
        }

        [Test]
        public async Task ClickAndWaitForRedirectAsync_DelegatesToAction()
        {
            var expectedPage = Substitute.For<IUnionPage>();
            _mockAction.ClickAndWaitForRedirectAsync<IUnionPage>(_mockLocator).Returns(expectedPage);
            var element = CreateElement();

            var result = await element.ClickAndWaitForRedirectAsync<IUnionPage>();

            result.Should().BeSameAs(expectedPage);
        }

        [Test]
        public async Task ClickAndWaitForAlertAsync_DelegatesToAction()
        {
            var expectedModal = Substitute.For<IUnionModal>();
            _mockAction.ClickAndWaitForAlertAsync<IUnionModal>(_mockLocator).Returns(expectedModal);
            var element = CreateElement();

            var result = await element.ClickAndWaitForAlertAsync<IUnionModal>();

            result.Should().BeSameAs(expectedModal);
        }

        [Test]
        public void GetByRole_DelegatesToRoot()
        {
            var roleLocator = Substitute.For<ILocator>();
            _mockLocator.GetByRole(AriaRole.Button, Arg.Any<LocatorGetByRoleOptions>()).Returns(roleLocator);
            var element = CreateElement();

            var result = element.GetByRole(AriaRole.Button);

            result.Should().BeSameAs(roleLocator);
        }

        [Test]
        public void GetByText_DelegatesToRoot()
        {
            var textLocator = Substitute.For<ILocator>();
            _mockLocator.GetByText("Submit", Arg.Any<LocatorGetByTextOptions>()).Returns(textLocator);
            var element = CreateElement();

            var result = element.GetByText("Submit");

            result.Should().BeSameAs(textLocator);
        }

        [Test]
        public void Filter_DelegatesToRoot()
        {
            var filteredLocator = Substitute.For<ILocator>();
            _mockLocator.Filter(Arg.Any<LocatorFilterOptions>()).Returns(filteredLocator);
            var element = CreateElement();

            var result = element.Filter(new LocatorFilterOptions { HasText = "test" });

            result.Should().BeSameAs(filteredLocator);
        }

        [Test]
        public async Task ClickAndWaitForAsync_DelegatesToAction()
        {
            var expectedComponent = new UnionElement(_mockPage, ".result");
            _mockAction.ClickAndWaitForAsync<UnionElement>(_mockLocator).Returns(expectedComponent);
            var element = CreateElement();

            var result = await element.ClickAndWaitForAsync<UnionElement>();

            result.Should().BeSameAs(expectedComponent);
            await _mockAction.Received(1).ClickAndWaitForAsync<UnionElement>(_mockLocator);
        }

        [Test]
        public void Constructor_WhenRootScssIsNull_ThrowsArgumentException()
        {
            var act = () => new UnionElement(_mockPage, null);

            act.Should().Throw<ArgumentException>()
                .WithParameterName("rootScss");
        }

        [Test]
        public void Constructor_WhenRootScssIsEmpty_ThrowsArgumentException()
        {
            var act = () => new UnionElement(_mockPage, "");

            act.Should().Throw<ArgumentException>()
                .WithParameterName("rootScss");
        }

        [Test]
        public void Constructor_WhenRootScssIsWhitespace_ThrowsArgumentException()
        {
            var act = () => new UnionElement(_mockPage, "   ");

            act.Should().Throw<ArgumentException>()
                .WithParameterName("rootScss");
        }
    }
}
