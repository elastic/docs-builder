/**
 * Factories for `vi.mock` that several test files used to copy.
 *
 * `vi.mock` is hoisted above imports, so import the factory lazily:
 *
 *   vi.mock('idb-keyval', async () => (await import('../../test-utils/mocks')).idbKeyval())
 */

/** IndexedDB is not available in the jsdom test environment. */
export const idbKeyval = () => ({
    get: vi.fn().mockResolvedValue(null),
    set: vi.fn().mockResolvedValue(undefined),
    del: vi.fn().mockResolvedValue(undefined),
    createStore: vi.fn().mockReturnValue({}),
})

/** Every export of `telemetry/logging`, so code under test never hits an undefined function. */
export const logging = () => ({
    logInfo: vi.fn(),
    logWarn: vi.fn(),
    logError: vi.fn(),
    logDebug: vi.fn(),
})
