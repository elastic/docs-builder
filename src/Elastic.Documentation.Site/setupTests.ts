import '@testing-library/jest-dom'

// This prevets the error:
// TypeError: Cannot set properties of undefined (setting 'diff')
jest.mock('@elastic/eui/lib/components/text_diff', () => ({
    EuiTextDiff: () => null,
}))

// Mock htmx.org to prevent XPath errors in jsdom test environment
// jsdom's XPath implementation is incomplete and causes "You must provide an XPath result type" errors
jest.mock('htmx.org', () => ({
    __esModule: true,
    default: {
        process: jest.fn(),
        on: jest.fn(),
        off: jest.fn(),
        trigger: jest.fn(),
        ajax: jest.fn(),
        reload: jest.fn(),
    },
}))

// Polyfill ResizeObserver for jsdom test environment
// ResizeObserver is not available in jsdom by default
global.ResizeObserver = class ResizeObserver {
    observe() {}
    unobserve() {}
    disconnect() {}
}
