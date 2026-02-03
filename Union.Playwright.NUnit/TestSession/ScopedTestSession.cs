using Microsoft.Extensions.DependencyInjection;
using Union.Playwright.NUnit.Core;

namespace Union.Playwright.NUnit.TestSession
{
    public class ScopedTestSession : IDisposable, IAsyncDisposable
    {
        public ITestSession Session { get; }
        private readonly AsyncServiceScope _scope;

        public ScopedTestSession(ITestSession session, AsyncServiceScope scope)
        {
            this.Session = session;
            _scope = scope;
        }

        public void Dispose() => _scope.Dispose();

        public ValueTask DisposeAsync() => _scope.DisposeAsync();
    }
}
