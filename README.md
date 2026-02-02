# Union.Playwright.NUnit

A .NET 9.0 testing framework built on top of **Microsoft Playwright** and **NUnit**. It adds Page Object Model infrastructure with URL-based routing, automatic page resolution, reusable components, and dependency injection — so you can stop hand-wiring navigation and page state and focus on writing tests.

## Why migrate from raw Playwright?

| Concern | Raw Playwright | Union.Playwright.NUnit |
|---|---|---|
| **Page identity** | You track which page is open manually | Framework matches the current URL to a registered page type automatically |
| **Navigation** | `await page.GotoAsync("/users/42")` — magic strings everywhere | `await service.Go.ToPage<UserProfilePage>()` — type-safe, refactor-friendly |
| **Path parameters** | Assembled by hand: `$"/users/{id}/orders/{orderId}"` | Declared once in `AbsolutePath`, extracted into `Data` dictionary on match |
| **Query parameters** | Parsed manually from URL | Extracted automatically into `Params` dictionary |
| **Page components** | Locators scattered across test methods | `[UnionInit(".selector")]` fields initialized automatically when the page activates |
| **Reusable lists** | Repeated boilerplate for "find all items, iterate" | `ListBase<T>` + `ItemBase` give you `GetItemsAsync()`, `FindSingleAsync()`, `FindRandomAsync()` |
| **Multi-service tests** | Separate setup for each origin | Register multiple `UnionService` instances; the framework resolves which service owns the current URL |
| **Parallel isolation** | Manual context management | `TestAwareServiceContextsPool` provides one browser context per service, thread-safe |
| **DI** | Not built-in | Full `Microsoft.Extensions.DependencyInjection` via `TestSessionProvider` |

---

## Installation

```bash
dotnet add package Union.Playwright.NUnit
```

Then install Playwright browsers:

```bash
pwsh bin/Debug/net9.0/playwright.ps1 install
```

---

## Core concepts

### 1. Pages

A page is a class that maps to a URL pattern.

```csharp
public abstract class MyAppPage : UnionPage { }

public class LoginPage : MyAppPage
{
    public override string AbsolutePath => "/login";
}

public class UserProfilePage : MyAppPage
{
    public override string AbsolutePath => "/users/{userId}";

    // Path parameters are extracted automatically
    public string UserId => Data["userId"];
}

public class SearchResultsPage : MyAppPage
{
    public override string AbsolutePath => "/search";

    // Query parameters (?q=playwright) extracted automatically
    public string Query => Params["q"];
}
```

Key points:
- `AbsolutePath` supports `{paramName}` placeholders — matched segments populate the `Data` dictionary.
- Query string values populate the `Params` dictionary.
- Override `WaitLoadedAsync()` to add custom wait logic (spinners, skeleton screens) that runs after every navigation.

### 2. Services

A service represents one application under test (one base URL).

```csharp
public class MyAppService : UnionService<MyAppPage>
{
    public MyAppService(IServiceContextsPool pool) : base(pool) { }

    public override string BaseUrl => "https://myapp.example.com";
}
```

On construction the service **automatically discovers and registers every non-abstract page class** that derives from the base page type (`MyAppPage`) via reflection. No manual registration needed.

### 3. Navigation (`Go`)

```csharp
// Navigate by type — URL is assembled from the page's AbsolutePath + BaseUrl
var loginPage = await myApp.Go.ToPage<LoginPage>();

// Navigate to a page with path parameters pre-filled
var profile = new UserProfilePage { Data = { ["userId"] = "42" } };
await myApp.Go.ToPage(profile);
// Browser navigates to https://myapp.example.com/users/42

// Raw URL navigation (still actualizes state afterward)
await myApp.Go.ToUrl("https://myapp.example.com/settings");

// Browser controls
await myApp.Go.Refresh();
await myApp.Go.Back();
```

After every navigation the framework:
1. Matches the resulting URL against all registered pages.
2. Creates a page instance with extracted `Data` and `Params`.
3. Calls `page.Activate()` — which triggers component initialization.
4. Calls `page.WaitLoadedAsync()`.

### 4. State

```csharp
// Check what page the browser is on
bool isLogin = myApp.State.PageIs<LoginPage>();

// Get the current page as a specific type (null if it doesn't match)
var profile = myApp.State.PageAs<UserProfilePage>();
Console.WriteLine(profile?.UserId); // "42"
```

### 5. Components

Components are reusable UI building blocks that attach to pages (or to other components) via the `[UnionInit]` attribute.

```csharp
// A simple element wrapper
public class Element : ComponentBase
{
    public Element(IUnionPage parentPage, string rootScss)
        : base(parentPage, rootScss) { }
}

// A page with auto-initialized components
public class QuestionsPage : MyAppPage
{
    public override string AbsolutePath => "/questions";

    [UnionInit("div#questions")]
    public QuestionList Questions { get; set; }
}
```

When the page is activated, every field and property marked with `[UnionInit]` is instantiated automatically. The selector argument is scoped relative to the page's (or parent component's) `RootScss`, so components compose cleanly.

#### Lists and items

`ListBase<T>` and `ItemBase` handle the common "collection of repeated elements" pattern:

```csharp
public class QuestionList : ListBase<QuestionItem>
{
    public override string ItemIdScss => "div.s-post-summary";
    public override string IdAttribute => "data-post-id";

    public QuestionList(IUnionPage parentPage, string rootScss)
        : base(parentPage, rootScss) { }
}

public class QuestionItem : ItemBase
{
    public override string ItemScss =>
        $"div.s-post-summary[data-post-id='{this.Id}']";

    [UnionInit("h3 a")]
    public Element Title { get; set; }

    public QuestionItem(IContainer container, string id)
        : base(container, id) { }
}
```

Usage in tests:

```csharp
var page = await myApp.Go.ToPage<QuestionsPage>();

List<string> ids = await page.Questions.GetIdsAsync();
List<QuestionItem> items = await page.Questions.GetItemsAsync();
QuestionItem single = await page.Questions.FindSingleAsync();
QuestionItem random = await page.Questions.FindRandomAsync();

bool visible = await single.Title.IsVisibleAsync();
```

### 6. SCSS selectors

The framework includes a custom selector language that compiles to both CSS and XPath. It extends standard CSS syntax with XPath-only features:

| Syntax | Meaning |
|---|---|
| `div .child` | Descendant (CSS) |
| `div > .child` | Direct child (CSS) |
| `#id`, `.class` | Standard CSS selectors |
| `[attr='value']` | Attribute equals |
| `[attr~'value']` | Attribute contains |
| `[text()='Login']` | XPath text match |
| `['~partial']` | Contains text |
| `[last()]` | XPath last() |
| `[1]`, `[2]` | Positional index |

CSS-compatible selectors are emitted as CSS; others fall back to XPath automatically. Use `InnerScss()` in components to scope relative selectors to the component root.

---

## Test setup

### 1. Define a test session

A test session groups the services available to a test:

```csharp
public class MyTestSession : ITestSession
{
    public MyAppService MyApp { get; }

    public MyTestSession(MyAppService myApp)
    {
        this.MyApp = myApp;
    }

    public List<IUnionService> GetServices() => new() { this.MyApp };
}
```

### 2. Define a session provider

The provider configures dependency injection:

```csharp
public class MySessionProvider : TestSessionProvider<MyTestSession>
{
    public static readonly MySessionProvider Instance = new();

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<MyAppService>();
    }
}
```

### 3. Write tests

```csharp
[TestFixture]
public class LoginTests : UnionTest<MyTestSession>
{
    protected override TestSessionProvider<MyTestSession> GetSessionProvider()
        => MySessionProvider.Instance;

    [Test]
    public async Task NavigateToLogin()
    {
        var app = this.Session.MyApp;

        var loginPage = await app.Go.ToPage<LoginPage>();

        app.State.PageIs<LoginPage>().Should().BeTrue();
        loginPage.Should().NotBeNull();
    }
}
```

`UnionTest<T>`:
- Inherits from Playwright's `PageTest` — you still have `this.Page`, `this.Context`, `this.Browser`.
- `[SetUp]` creates a scoped session; `[TearDown]` disposes it.
- Access the session via `this.Session`.
- Retrieve a service via `this.GetService<TService>()`.

---

## Migration guide: before and after

### Navigating to a page

**Before:**
```csharp
await page.GotoAsync("https://myapp.com/login");
// hope the URL is correct, no compile-time safety
```

**After:**
```csharp
var loginPage = await myApp.Go.ToPage<LoginPage>();
// URL assembled from BaseUrl + AbsolutePath, returns typed page object
```

### Extracting path parameters

**Before:**
```csharp
await page.GotoAsync($"https://myapp.com/users/{userId}/orders/{orderId}");
// later, to verify:
var url = page.Url;
var segments = new Uri(url).Segments; // manual parsing
```

**After:**
```csharp
public class OrderPage : MyAppPage
{
    public override string AbsolutePath => "/users/{userId}/orders/{orderId}";
    public string UserId => Data["userId"];
    public string OrderId => Data["orderId"];
}

var orderPage = myApp.State.PageAs<OrderPage>();
var userId = orderPage.UserId; // extracted automatically from URL
```

### Page components

**Before:**
```csharp
var submitBtn = page.Locator("#form .submit-button");
var nameInput = page.Locator("#form input[name='username']");
// locators duplicated across every test that touches this page
```

**After:**
```csharp
public class LoginPage : MyAppPage
{
    public override string AbsolutePath => "/login";

    [UnionInit("#form .submit-button")]
    public Element SubmitButton { get; set; }

    [UnionInit("#form input[name='username']")]
    public Element NameInput { get; set; }
}

// In test — components ready after navigation
var loginPage = await myApp.Go.ToPage<LoginPage>();
await loginPage.SubmitButton.IsVisibleAsync();
```

### Repeated element lists

**Before:**
```csharp
var items = page.Locator(".item");
var count = await items.CountAsync();
for (int i = 0; i < count; i++)
{
    var title = await items.Nth(i).Locator("h3").TextContentAsync();
    // ...
}
```

**After:**
```csharp
var items = await page.ItemList.GetItemsAsync();
foreach (var item in items)
{
    var visible = await item.Title.IsVisibleAsync();
    // each item is a strongly-typed object with its own components
}
```

### Knowing which page you're on

**Before:**
```csharp
Assert.That(page.Url, Does.Contain("/dashboard"));
// fragile string matching
```

**After:**
```csharp
myApp.State.PageIs<DashboardPage>().Should().BeTrue();
// URL pattern matching with parameter extraction
```

---

## API reference

### UnionPage

| Member | Description |
|---|---|
| `abstract string AbsolutePath` | URL path pattern, e.g. `/users/{userId}` |
| `Dictionary<string, string> Data` | Extracted path parameters |
| `Dictionary<string, string> Params` | Extracted query parameters |
| `IPage PlaywrightPage` | Underlying Playwright page |
| `virtual Task WaitLoadedAsync()` | Override for custom load waits |
| `virtual string RootScss` | Root selector for component scoping |

### UnionService\<T\>

| Member | Description |
|---|---|
| `abstract string BaseUrl` | Application base URL |
| `IBrowserGo Go` | Navigation API |
| `IBrowserState State` | Current page state |

### IBrowserGo

| Method | Description |
|---|---|
| `Task<T> ToPage<T>()` | Navigate to page by type |
| `Task ToPage(IUnionPage page)` | Navigate to pre-configured page instance |
| `Task ToUrl(string url)` | Navigate to raw URL |
| `Task Refresh()` | Refresh current page |
| `Task Back()` | Browser back |

### IBrowserState

| Member | Description |
|---|---|
| `IUnionPage? Page` | Current resolved page |
| `T? PageAs<T>()` | Current page cast to T, or null |
| `bool PageIs<T>()` | Check if current page matches type |
| `void Actualize(IPage page)` | Re-resolve current URL to a page |

### ComponentBase

| Member | Description |
|---|---|
| `IUnionPage ParentPage` | Owning page |
| `string RootScss` | Scoped root selector |
| `ILocator Root` | Playwright locator for root element |
| `IPage PlaywrightPage` | Underlying Playwright page |
| `Task<bool> IsVisibleAsync()` | Check component visibility |
| `string InnerScss(string relative)` | Build scoped child selector |

### ListBase\<T\>

| Member | Description |
|---|---|
| `abstract string ItemIdScss` | Selector for item elements |
| `virtual string IdAttribute` | Attribute to use as item ID (null = text content) |
| `Task<List<string>> GetIdsAsync()` | Get all item IDs |
| `Task<List<T>> GetItemsAsync()` | Get all items as typed objects |
| `Task<T> FindSingleAsync()` | Get the single item (asserts exactly one) |
| `Task<T> FindRandomAsync()` | Get a random item |

### Attributes

| Attribute | Description |
|---|---|
| `[UnionInit(selector)]` | Auto-initialize this field/property as a component with the given selector |
| `[UnionInit(selector, ComponentName = "...")]` | Same, with a display name |
| `[UnionInit(selector, FrameXcss = "...")]` | Same, scoped to an iframe |
