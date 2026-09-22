import {
    connectToDiagnosticsStream,
    disconnectFromDiagnosticsStream,
} from './diagnosticsStreamClient'

describe('diagnosticsStreamClient', () => {
    const originalFetch = globalThis.fetch

    beforeEach(() => {
        jest.useFakeTimers()
        disconnectFromDiagnosticsStream()
        globalThis.fetch = jest.fn().mockResolvedValue({
            ok: true,
            json: async () => ({
                type: 'state',
                timestamp: 1,
                errors: 0,
                warnings: 0,
                hints: 2,
                status: 'complete',
                diagnostics: [],
            }),
        })
    })

    afterEach(() => {
        disconnectFromDiagnosticsStream()
        globalThis.fetch = originalFetch
        Object.defineProperty(document, 'readyState', {
            configurable: true,
            get: () => 'complete',
        })
        jest.useRealTimers()
    })

    it('polls /_api/diagnostics/state after load instead of opening EventSource', async () => {
        connectToDiagnosticsStream()
        expect(globalThis.fetch).not.toHaveBeenCalled()

        jest.runOnlyPendingTimers()
        await Promise.resolve()

        expect(globalThis.fetch).toHaveBeenCalledWith(
            '/_api/diagnostics/state',
            expect.objectContaining({ cache: 'no-store' })
        )
    })

    it('starts one poll loop when connect runs twice before load', async () => {
        Object.defineProperty(document, 'readyState', {
            configurable: true,
            get: () => 'loading',
        })

        connectToDiagnosticsStream()
        connectToDiagnosticsStream()
        window.dispatchEvent(new Event('load'))
        jest.runOnlyPendingTimers()
        await Promise.resolve()
        await Promise.resolve()
        await Promise.resolve()

        expect(globalThis.fetch).toHaveBeenCalledTimes(1)

        jest.advanceTimersByTime(1500)
        await Promise.resolve()
        await Promise.resolve()

        expect(globalThis.fetch).toHaveBeenCalledTimes(2)
    })

    it('stops polling on disconnect', async () => {
        connectToDiagnosticsStream()
        jest.runOnlyPendingTimers()
        await Promise.resolve()
        disconnectFromDiagnosticsStream()
        ;(globalThis.fetch as jest.Mock).mockClear()

        jest.advanceTimersByTime(5000)
        await Promise.resolve()

        expect(globalThis.fetch).not.toHaveBeenCalled()
    })
})
