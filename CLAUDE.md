# Code Style Guidelines

This project follows strict C# coding standards. All code must adhere to these guidelines.

## Formatting

- **Indentation**: Tabs (4-space width)
- **Max line length**: 160 characters
- **Brace style**: Allman (new line before all braces)
- **File-scoped namespaces**: Always use `namespace Foo;` not `namespace Foo { }`
- **Trailing whitespace**: Never
- **Final newline**: Always

### File Header (Required)

Every `.cs` file must start with:
```csharp
// Licensed to Elasticsearch B.V underaone or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
```

## Naming Conventions

| Element                 | Style                  | Example                               |
|-------------------------|------------------------|---------------------------------------|
| Private fields          | `_camelCase`           | `private readonly IService _service;` |
| Constants               | `PascalCase`           | `public const int MaxRetries = 3;`    |
| Public members          | `PascalCase`           | `public string Name { get; }`         |
| Locals/parameters       | `camelCase`            | `var userName = GetName();`           |
| Local functions         | `PascalCase`           | `void ProcessItem() { }`              |
| Async methods (public)  | `PascalCase` + `Async` | `public Task<T> GetDataAsync()`       |
| Async methods (private) | `PascalCase`           | `private Task<T> GetData()`           |

## Language Features (REQUIRED)

These are **error-level** requirements:

- **Always use `var`** — never write explicit types
- **No `this.` qualifier** — never prefix with `this`
- **Use language keywords** — `int` not `Int32`, `string` not `String`
- **Object/collection initializers** — always use when applicable
- **Null propagation** — use `?.` and `??` operators
- **Pattern matching** — prefer over `is`/`as` with cast/null checks
- **Inlined variable declarations** — use `out var x`
- **Throw expressions** — use when appropriate
- **Conditional delegate calls** — use `?.Invoke()`

## Expression Bodies

- **Prefer `=>` always** for all members when possible
- **Multi-line expressions**: Put newline after `=>`
- **Keep under 160 chars** for single-line

```csharp
// GOOD - single line
public string Name => _name;

// GOOD - multi-line with newline after =>
public string FormattedName =>
	$"{FirstName} {LastName}";

// BAD - too long on one line
public string VeryLongPropertyName => SomeMethod(argument1, argument2, argument3);
```

## Class Structure Order

1. **Fields** (private first)
2. **Constructors** (prefer primary constructors)
3. **Properties**
4. **Methods** (grouped by related functionality, NOT by visibility)

```csharp
public class UserService(IRepository repository, ILogger logger)
{
	private readonly Dictionary<string, User> _cache = [];

	public int CacheSize => _cache.Count;

	// Related methods grouped together
	public User GetUser(string id) => ...
	private User LoadFromCache(string id) => ...
	private void UpdateCache(User user) { ... }

	// Another logical group
	public void DeleteUser(string id) => ...
	private void InvalidateCache(string id) => ...
}
```

## Modern C# Preferences

- **Primary constructors** for dependency injection
- **Records liberally** for immutable data types
- **Collection expressions**: Use `[]` syntax (C# 12+)
- **Switch expressions** over if/else chains
- **Early returns** with guard clauses
- **Using declarations** (not using statements with braces)
- **Empty returns**: Use `[]` not `Array.Empty<T>()`

```csharp
// GOOD
public record UserDto(string Id, string Name, string Email);

// GOOD
var items = [];
return [];

// GOOD
using var stream = File.OpenRead(path);

// BAD
using (var stream = File.OpenRead(path)) { }
```

## Object & Collection Initializers

- **Single line** when it fits under 160 chars
- **Multi-line** only when exceeding line length

```csharp
// GOOD - fits on one line
var point = new { X = 10, Y = 20 };
var user = new UserDto { Id = "123", Name = "John" };

// GOOD - exceeds line length, so multi-line
var config = new AppConfiguration
{
	ConnectionString = "Server=localhost;Database=mydb",
	MaxRetries = 3,
	TimeoutSeconds = 30
};
```

## Multi-line Invocations

When a method call spans multiple lines, the closing parenthesis goes on its own line, indented to the method level:

```csharp
// CORRECT
var result = DoSomething(
	argument1,
	argument2,
	argument3
);

// CORRECT - chained
var query = items
	.Where(x => x.IsActive)
	.Select(x => new { x.Id, x.Name })
	.ToList();

// WRONG - closing paren not on own line
var result = DoSomething(argument1,
	argument2);

// WRONG - closing paren over-indented
var result = DoSomething(
	argument1
);
```

## LINQ Style

- **Method syntax** preferred over query syntax
- **Each method on new line** for readability
- **Query syntax** only for complex joins/multiple sources

```csharp
// GOOD
var activeUsers = users
	.Where(u => u.IsActive)
	.OrderBy(u => u.Name)
	.Select(u => u.Email)
	.ToList();

// ACCEPTABLE - complex join
var result =
	from order in orders
	join customer in customers on order.CustomerId equals customer.Id
	where customer.IsActive
	select new { order.Id, customer.Name };
```

## Strings

- **Interpolation** for single-line: `$"Hello {name}"`
- **Raw string literals** for multi-line

```csharp
// GOOD
var message = $"User {user.Name} logged in at {DateTime.Now}";

// GOOD
var json = """
	{
		"name": "test",
		"value": 123
	}
	""";
```

## Error Handling

- **Minimal try-catch** — catch at boundaries only
- **Let exceptions propagate** through the stack
- **No empty catch blocks**

## Dispose Pattern

- **GC.SuppressFinalize only when needed** — only if the class has a finalizer or manages unmanaged resources
- **Most IDisposable implementations don't need it**

```csharp
// GOOD - simple disposable, no finalizer
public class SimpleResource : IDisposable
{
	private readonly Stream _stream;

	public void Dispose() => _stream.Dispose();
}

// GOOD - has finalizer/unmanaged resources
public class UnmanagedResource : IDisposable
{
	private IntPtr _handle;

	~UnmanagedResource() => Dispose(false);

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);  // Only needed here
	}

	protected virtual void Dispose(bool disposing) { ... }
}
```

## Comments & XML Documentation

- **Self-documenting code** — comments explain "why" not "what"
- **XML docs** for public API members
- **Explain complex logic** and business rules
- **Single-line summary** when it fits — only `<summary>` is needed

```csharp
/// <summary>Retrieves user by ID with caching.</summary>
public User GetUser(string id)
{
	// Check cache first to avoid expensive DB call
	if (_cache.TryGetValue(id, out var cached))
		return cached;

	return LoadFromDatabase(id);
}

// GOOD - single line summary
/// <summary>Gets the user's display name.</summary>
public string DisplayName => $"{FirstName} {LastName}";

// GOOD - multi-line only when needed
/// <summary>
/// Performs a complex calculation that requires detailed explanation
/// across multiple lines because the logic is non-obvious.
/// </summary>
public decimal CalculateScore() => ...
```

## Testing

- **Framework**: TUnit (for new projects)
- **Assertions**: AwesomeAssertions (fluent style)
- **Method naming**: `MethodName_Scenario_Expected`
- **Structure**: Keep tests minimal and focused

```csharp
[Test]
public void GetUser_WithValidId_ReturnsUser()
{
	var user = _service.GetUser("123");

	user.Should().NotBeNull();
	user.Id.Should().Be("123");
}

[Test]
public void GetUser_WithInvalidId_ThrowsNotFoundException()
{
	var act = () => _service.GetUser("invalid");

	act.Should().Throw<NotFoundException>();
}
```

## Anti-Patterns to AVOID

| Never Do This                        | Do This Instead                           |
|--------------------------------------|-------------------------------------------|
| `#region` directives                 | Organize with partial classes or refactor |
| Multiple statements per line         | One statement per line                    |
| `new List<string>()` when type known | `new()` or `[]`                           |
| `this.field`                         | `_field` or just `field`                  |
| `String`, `Int32`                    | `string`, `int`                           |
| `if (x != null) x.Method()`          | `x?.Method()`                             |

## Modifier Order

Always order modifiers as:
```
public, private, protected, internal, static, extern, new, virtual, abstract, sealed, override, readonly, unsafe, volatile, async
```

## Nullable Reference Types

- **Strict nullable annotations** — always annotate with `?`
- **Use null-forgiving `!` sparingly** — only when obviously safe
- **Prefer null checks** and pattern matching

---

# Best Practices

## Early Returns & Guard Clauses

- **Return early** to avoid deep nesting
- **Handle error/edge cases first**, then the happy path
- **False/unspecified should be the default** state

```csharp
// GOOD - early returns, flat structure
public User GetUser(string id)
{
	if (string.IsNullOrEmpty(id))
		return null;

	if (!_cache.TryGetValue(id, out var user))
		return LoadFromDatabase(id);

	return user;
}

// BAD - nested structure
public User GetUser(string id)
{
	if (!string.IsNullOrEmpty(id))
	{
		if (_cache.TryGetValue(id, out var user))
		{
			return user;
		}
		else
		{
			return LoadFromDatabase(id);
		}
	}
	return null;
}
```

## Method Parameters

- **Max 4 parameters** — use a record/options object for more
- **Boolean parameters**: Always use named arguments
- **Sensible defaults** — methods should work with minimal config

```csharp
// GOOD - record for many parameters
public record SearchOptions(
	string Query,
	int Page = 1,
	int PageSize = 20,
	bool IncludeDeleted = false,
	SortOrder Sort = SortOrder.Relevance
);

public SearchResult Search(SearchOptions options) => ...

// GOOD - named boolean arguments
var result = Process(data, validateInput: true);

// BAD - too many parameters
public void Send(string to, string from, string subject, string body, bool html, bool priority, int retries) => ...

// BAD - unnamed boolean
var result = Process(data, true);  // What does true mean?
```

## Complexity & Method Design

- **Cyclomatic complexity limit**: Max 5-7 branches per method
- **Single responsibility**: One method does one thing
- **Extract early**: Break complex logic into smaller, named methods

```csharp
// GOOD - low complexity, clear intent
public bool CanProcessOrder(Order order) =>
	order.Status == OrderStatus.Pending &&
	order.Items.Count > 0 &&
	IsPaymentValid(order) &&
	IsInventoryAvailable(order);

// BAD - high complexity, hard to follow
public bool CanProcessOrder(Order order)
{
	if (order.Status == OrderStatus.Pending)
	{
		if (order.Items.Count > 0)
		{
			if (order.Payment != null && order.Payment.IsValid)
			{
				foreach (var item in order.Items)
				{
					if (_inventory.GetStock(item.ProductId) < item.Quantity)
						return false;
				}
				return true;
			}
		}
	}
	return false;
}
```

## Defensive Programming

- **Validate at boundaries** — public API entry points
- **Fail fast** — throw early, don't hide errors
- **Trust internal code** — don't over-validate private methods

```csharp
// GOOD - validate at public API boundary
public void ProcessOrder(Order order)
{
	ArgumentNullException.ThrowIfNull(order);
	if (order.Items.Count == 0)
		throw new ArgumentException("Order must have items", nameof(order));

	ProcessInternal(order);  // Internal method trusts the input
}

private void ProcessInternal(Order order)
{
	// No validation needed - called only from validated context
	foreach (var item in order.Items)
		ProcessItem(item);
}
```

## Immutability

- **Immutable by default** — use `readonly`, `init`, records
- **Mutate only when necessary** — performance or API requirements

```csharp
// GOOD - immutable record
public record UserCreated(string UserId, string Email, DateTime CreatedAt);

// GOOD - readonly fields
public class UserService(IRepository repository)
{
	private readonly Dictionary<string, User> _cache = [];  // readonly reference, mutable content OK
}

// GOOD - init-only properties
public class Configuration
{
	public required string ConnectionString { get; init; }
	public int MaxRetries { get; init; } = 3;
}
```

## Composition Over Inheritance

- **Prefer interfaces** and delegation over base classes
- **Avoid deep inheritance hierarchies**
- **Sealed by default** unless designed for extension

```csharp
// GOOD - composition via interfaces
public interface IValidator<T>
{
	bool Validate(T item);
}

public class OrderProcessor(IValidator<Order> validator, IRepository repository)
{
	public void Process(Order order)
	{
		if (!validator.Validate(order))
			throw new ValidationException();
		repository.Save(order);
	}
}

// AVOID - inheritance hierarchy
public abstract class BaseProcessor { }
public abstract class EntityProcessor : BaseProcessor { }
public class OrderProcessor : EntityProcessor { }  // Too deep
```

## Async Patterns

- **Async all the way** — never block with `.Result` or `.Wait()`
- **ConfigureAwait(false)** in library code
- **ValueTask for hot paths** — when method often completes synchronously
- **Always accept CancellationToken** — propagate down the call stack

```csharp
// GOOD - async all the way with cancellation
public async Task<User> GetUserAsync(string id, CancellationToken ct = default)
{
	ct.ThrowIfCancellationRequested();

	var user = await _repository
		.GetByIdAsync(id, ct)
		.ConfigureAwait(false);

	return user;
}

// GOOD - ValueTask for cached results
public ValueTask<User> GetUserAsync(string id)
{
	if (_cache.TryGetValue(id, out var user))
		return ValueTask.FromResult(user);

	return new ValueTask<User>(LoadUserAsync(id));
}

// BAD - blocking on async
public User GetUser(string id) =>
	GetUserAsync(id).Result;  // Never do this!
```

## Return Values & Null Handling

- **Never return null for collections** — return empty `[]`
- **TryGet pattern for lookups** — `bool TryGet(out T value)`
- **Don't use Option/Result types** in C# — use nullable reference types

```csharp
// GOOD - empty collection, not null
public IReadOnlyList<User> GetUsers(string filter) =>
	_users.Where(u => u.Name.Contains(filter)).ToList() ?? [];

// GOOD - TryGet pattern
public bool TryGetUser(string id, out User user) =>
	_cache.TryGetValue(id, out user);

// GOOD - nullable for single items
public User? FindUser(string id) =>
	_users.FirstOrDefault(u => u.Id == id);
```

## Code Reuse

- **Rule of three** — don't abstract until you have 3 concrete uses
- **Prefer duplication over wrong abstraction**
- **Extract after patterns emerge**, not before

## Dependency Injection

- **Constructor injection only** — no property or method injection
- **Avoid static methods** for testability (except pure functions)
- **IOptions<T> pattern** for configuration

```csharp
// GOOD - constructor injection with options
public class EmailService(IOptions<EmailOptions> options, ISmtpClient client)
{
	private readonly EmailOptions _options = options.Value;

	public Task SendAsync(Email email) => ...
}
```

## Fluent APIs & Method Chaining

- **Embrace fluent builders** for configuration
- **Return `this`** for chainable methods

```csharp
// GOOD - fluent builder
var query = new QueryBuilder()
	.From("logs-*")
	.Where(q => q.Term("level", "error"))
	.Sort("@timestamp", SortOrder.Desc)
	.Size(100)
	.Build();
```

## Extension Methods

- **For types you don't own** (framework/library types)
- **For interface composition** (adding behavior to interfaces)
- **Not for your own classes** — use instance methods

```csharp
// GOOD - extending framework type
public static class StringExtensions
{
	public static bool IsNullOrEmpty(this string? value) =>
		string.IsNullOrEmpty(value);
}

// GOOD - extending interface
public static class QueryableExtensions
{
	public static IQueryable<T> WhereIf<T>(
		this IQueryable<T> query,
		bool condition,
		Expression<Func<T, bool>> predicate
	) =>
		condition ? query.Where(predicate) : query;
}
```

## Logging

- **Structured logging** with semantic properties
- **Log at appropriate levels** — errors for errors, info for key events

```csharp
// GOOD - structured with properties
_logger.LogInformation("User {UserId} logged in from {IpAddress}", userId, ip);

// GOOD - appropriate level
_logger.LogError(ex, "Failed to process order {OrderId}", orderId);

// BAD - string concatenation
_logger.LogInformation("User " + userId + " logged in");
```

## Namespaces

- **Feature-based organization** — group by feature, not technical layer
- **Match logical structure**, not necessarily folder structure


## Resolve types

```csharp

// AVOID Reference types fully qualified inline
if (!typeDecl.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)))
    return false;

// GOOD always import the namespace with a using statement
if (!typeDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
    return false;
```
