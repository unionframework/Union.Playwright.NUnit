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
        public TestContainer(IUnionPage parentPage, string rootXcss = null)
            : base(parentPage, rootXcss) { }
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
        public void InnerXcss_ConcatenatesRootAndRelative()
        {
            var container = new TestContainer(_mockPage, "div.container");

            var result = container.InnerXcss("span.child");

            result.XPath.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void InnerXcss_FormatsArgsIntoRelativeXcss()
        {
            var container = new TestContainer(_mockPage, "div.container");

            var result = container.InnerXcss("span[data-id='{0}']", "test-id");

            result.XPath.Should().Contain("test-id");
        }
    }
}
