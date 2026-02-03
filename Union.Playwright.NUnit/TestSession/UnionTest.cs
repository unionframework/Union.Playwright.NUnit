using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Services;

namespace Union.Playwright.NUnit.TestSession
{
    public abstract class UnionTest<TSession> : PageTest where TSession : class, ITestSession
    {
        private readonly object _sessionLock = new();
        private ScopedTestSession? _scopedSession;
        private TSession? _session;

        protected TSession Session
        {
            get
            {
                if (_session == null)
                {
                    EnsureSession();
                }
                return _session!;
            }
        }

        protected abstract TestSessionProvider<TSession> GetSessionProvider();

        private void EnsureSession()
        {
            lock (_sessionLock)
            {
                if (_scopedSession != null) return;
                _scopedSession = this.GetSessionProvider().CreateTestSession(() => this.Page);
                _session = (TSession)_scopedSession.Session;
            }
        }

        [TearDown]
        public async Task UnionTearDown()
        {
            if (_scopedSession != null)
            {
                await _scopedSession.DisposeAsync();
                _scopedSession = null;
                _session = null;
            }
        }

        protected TService GetService<TService>() where TService : IUnionService
        {
            return this.Session.GetServices().OfType<TService>().First();
        }
    }
}
