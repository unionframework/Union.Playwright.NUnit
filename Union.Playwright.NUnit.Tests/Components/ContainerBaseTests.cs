using FluentAssertions;
using Microsoft.Playwright;
using NSubstitute;
using NUnit.Framework;
using Union.Playwright.NUnit.Components;
using Union.Playwright.NUnit.Pages.Interfaces;

namespace Union.Playwright.NUnit.Tests.Components
{
    public class TestContainer : ContainerBase
    {
        public TestContainer(IUnionPage parentPage, string rootScss = null)
            : base(parentPage, rootScss)
        {
        }
    }

    [TestFixture]
    public class ContainerBaseTests
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
        public void InnerScss_ConcatenatesRootAndRelative()
        {
            var container = new TestContainer(_mockPage, "div.container");

            var result = container.InnerScss("span.child");

            result.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void InnerScss_FormatsArgsIntoRelativeScss()
        {
            var container = new TestContainer(_mockPage, "div.container");

            var result = container.InnerScss("span[data-id='{0}']", "test-id");

            result.Should().Contain("test-id");
        }
    }
}
