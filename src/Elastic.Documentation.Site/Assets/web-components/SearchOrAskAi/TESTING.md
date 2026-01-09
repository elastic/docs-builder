# SearchOrAskAi Testing Documentation

This document describes the test coverage for the SearchOrAskAi feature.

## Summary

**24 tests** across 3 test files

| Test File                          | Tests | Focus                      |
| ---------------------------------- | ----- | -------------------------- |
| `AskAi/chat.store.test.ts`         | 2     | Chat state scenarios       |
| `AskAi/Chat.test.tsx`              | 12    | Chat UI & interactions     |
| `AskAi/ChatMessage.test.tsx`       | 10    | Message rendering & states |
| `NavigationSearch/navigationSearch.store.test.ts` | 4 | Navigation search state |

## Testing Philosophy

### ✅ What We Test (User-Observable Behavior)

- User interactions (typing, clicking, keyboard navigation)
- Visual feedback (loading states, error messages, empty states)
- Workflow orchestration (search → chat handoff)
- Data validation (empty input prevention)
- Conversation flow (question → response → follow-up)

### ❌ What We Avoid Testing (Implementation Details)

- Internal state structure (exact message object properties)
- Private helper functions
- Message tracking mechanisms (implementation detail for preventing duplicate API calls)
- **ThreadId consistency** - requires LLM backend integration, tested in E2E/system tests

### Why This Approach?

**Behavior-focused tests** are:

- ✅ More resilient to refactoring
- ✅ Test what users actually care about
- ✅ Easier to understand and maintain
- ✅ Less brittle (don't break on internal changes)

## Test Structure

### Store Tests

#### `AskAi/chat.store.test.ts` (2 tests)

Tests chat state management through **complete user scenarios**:

- ✅ **Complete conversation flow**: Question → AI response → follow-up question
- ✅ **Clear and restart**: New conversation behavior

**What's NOT tested**:

- ❌ ThreadId (requires LLM backend)
- ❌ Message tracking internals (implementation detail)

**Coverage**: Core conversation flows

#### `NavigationSearch/navigationSearch.store.test.ts` (4 tests)

Tests the navigation search state management:

- ✅ **setSearchTerm**: Updating search input
- ✅ **clearSearchTerm**: Resetting search
- ✅ **Initial state**: Default empty state

**Coverage**: 100% of store logic

**Note**: Navigation search store tests remain granular because the store is simple and these tests are fast.

### Component Tests

#### `AskAi/Chat.test.tsx` (12 tests)

Tests the main chat interface:

- ✅ **Empty state**: Welcome message and suggestions
- ✅ **Message display**: Showing conversation history via ChatMessageList
- ✅ **Input handling**: Typing and submitting questions
- ✅ **New conversation**: Starting fresh chat
- ✅ **Auto-focus**: Refocusing input after AI response
- ✅ **Validation**: Preventing empty submissions

**Note**: Uses mocks for `ChatMessageList` and `AskAiSuggestions` to isolate Chat component logic.

**Coverage**: Chat component orchestration and UI logic

#### `AskAi/ChatMessage.test.tsx` (10 tests)

Tests individual message rendering:

- ✅ **User messages**: Displaying user questions with user icon
- ✅ **AI messages (complete)**: Showing finished responses with Elastic logo
- ✅ **AI messages (streaming)**: Loading states during generation
- ✅ **AI messages (error)**: Error handling and display
- ✅ **Markdown rendering**: Formatted content display
- ✅ **Feedback buttons**: Thumbs up/down visibility

**What's NOT tested**:

- ❌ Copy button functionality (trusting EUI's `EuiCopy` component)
- ❌ Retry button (no retry mechanism currently implemented)

**Coverage**: Core message rendering behaviors

### Integration Test Coverage

#### What's Tested

- ✅ User can type in navigation search field
- ✅ User can type in chat input
- ✅ User can submit questions in chat
- ✅ Chat messages are rendered correctly
- ✅ AI responses transition from streaming → complete
- ✅ User can start new conversations
- ✅ Stores maintain state correctly

#### What's NOT Tested (requires E2E)

- ❌ Actual LLM API calls
- ❌ Real EventSource streaming
- ❌ Cross-browser compatibility
- ❌ Performance under load

## Running Tests

### Run all tests (with verbose output locally)

```bash
cd src/Elastic.Documentation.Site
npm test
```

**Local output** (verbose):

- ✅ Each test suite name
- ✅ Each individual test name
- ✅ Pass/fail status per test
- ✅ Test execution time
- ✅ Summary at the end

**CI output** (summary):

- ✅ Only final summary
- ✅ Cleaner logs for GitHub Actions

The test runner automatically detects CI environment and adjusts verbosity.

### Run tests in CI mode (locally)

```bash
npm run test:ci
```

This simulates CI behavior with summary-only output.

### Run tests in watch mode

```bash
npm run test:watch
```

Watch mode automatically re-runs tests when files change.

### Run tests with coverage report

```bash
npm run test:coverage
```

Generates coverage report in `coverage/` directory.

### Run specific test file

```bash
npm test -- Chat.test.tsx
```

### Run tests for SearchOrAskAi only

```bash
npm test -- SearchOrAskAi
```

### Debug tests in VS Code

```bash
npm run test:debug
```

Then attach VS Code debugger to the Node process.

## Test Configuration

Tests use:

- **Jest** - Test runner
- **React Testing Library** - Component testing utilities
- **@testing-library/user-event** - User interaction simulation
- **@testing-library/jest-dom** - Additional matchers

Configuration is in `jest.config.js` in the root of the Assets directory.

### CI Detection

The test runner automatically adjusts output based on environment:

```javascript
const isCI = process.env.CI === 'true'

// In jest.config.js:
reporters: isCI
  ? [['github-actions', { silent: false }], 'summary']  // CI: compact
  : [['github-actions', { silent: false }], 'default'], // Local: verbose
verbose: !isCI,
```

**In CI** (GitHub Actions, GitLab CI, etc.):

- Uses `summary` reporter for cleaner logs
- Less verbose output
- Easier to spot failures in CI logs

**Locally** (your machine):

- Uses `default` reporter with `verbose: true`
- Shows every test name
- Easier to debug individual test failures

## Writing New Tests

### For Store Tests

```typescript
import { chatStore } from './chat.store'
import { act } from 'react'

describe('New feature', () => {
  beforeEach(() => {
    act(() => {
      chatStore.getState().actions.clearChat()
    })
  })

  it('should do something', () => {
    act(() => {
      // Test store actions
    })

    expect(chatStore.getState()).toMatchObject({
      // Expected state
    })
  })
})
```

### For Component Tests

```typescript
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

describe('New component', () => {
    it('should render', () => {
        render(<Component />)
        expect(screen.getByText(/expected text/i)).toBeInTheDocument()
    })

    it('should handle interaction', async () => {
        const user = userEvent.setup()
        render(<Component />)

        await user.click(screen.getByRole('button'))

        expect(mockFunction).toHaveBeenCalled()
    })
})
```

## Mocking Guidelines

### Mock External Dependencies

- Mock `useLlmGateway` for AI interactions
- Mock EUI components if they cause issues
- Mock Zustand stores to control state

### Don't Mock

- React hooks (useState, useEffect, etc.)
- Internal utility functions
- Simple UI components

## Coverage Goals

| Area            | Target | Current |
| --------------- | ------ | ------- |
| Stores          | 95%+   | ~95%    |
| Core Components | 80%+   | ~85%    |
| Utilities/Hooks | 90%+   | TBD     |
| Integration     | 70%+   | TBD     |

## Known Test Gaps

1. **ChatMessageList** - Needs tests for streaming orchestration
2. **AskAiSuggestions** - Needs tests for suggestion clicks
3. **NavigationSearch** - Needs tests for autocomplete rendering
4. **useLlmGateway** - Needs unit tests for streaming logic

## Future Improvements

- [ ] Add E2E tests with Playwright
- [ ] Add visual regression tests
- [ ] Test accessibility (a11y)
- [ ] Test keyboard navigation
- [ ] Performance benchmarks
- [ ] API mocking with MSW
