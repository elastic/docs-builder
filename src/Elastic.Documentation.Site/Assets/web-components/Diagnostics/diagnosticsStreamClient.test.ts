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

    it('does not cancel an in-flight poll when the next tick is due', async () => {
        let signal: AbortSignal | undefined
        globalThis.fetch = jest
            .fn()
            .mockImplementation((_url, init: RequestInit) => {
                signal = init.signal ?? undefined
                return new Promise(() => undefined)
            })

        connectToDiagnosticsStream()
        jest.runOnlyPendingTimers()
        await Promise.resolve()

        jest.advanceTimersByTime(3000)
        await Promise.resolve()

        expect(globalThis.fetch).toHaveBeenCalledTimes(1)
        expect(signal?.aborted).toBe(false)
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
