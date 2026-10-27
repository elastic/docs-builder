import '@testing-library/jest-dom/vitest'
import { TextDecoder, TextEncoder } from 'util'

// Polyfill TextEncoder/TextDecoder for jsdom test environment
// Not available in jsdom by default; needed by OpenTelemetry's protobuf-based exporters
global.TextEncoder = TextEncoder as typeof global.TextEncoder
global.TextDecoder = TextDecoder as typeof global.TextDecoder

// This prevets the error:
// TypeError: Cannot set properties of undefined (setting 'diff')
vi.mock('@elastic/eui/lib/components/text_diff', () => ({
    EuiTextDiff: () => null,
}))

// Mock htmx.org to prevent XPath errors in jsdom test environment
// jsdom's XPath implementation is incomplete and causes "You must provide an XPath result type" errors
vi.mock('htmx.org', () => ({
    __esModule: true,
    default: {
        process: vi.fn(),
        on: vi.fn(),
        off: vi.fn(),
        trigger: vi.fn(),
        ajax: vi.fn(),
        reload: vi.fn(),
    },
}))

// Polyfill ResizeObserver for jsdom test environment
// ResizeObserver is not available in jsdom by default
global.ResizeObserver = class ResizeObserver {
    observe() {}
    unobserve() {}
    disconnect() {}
}
