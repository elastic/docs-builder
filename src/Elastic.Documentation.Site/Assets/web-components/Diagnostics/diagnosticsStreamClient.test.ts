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
