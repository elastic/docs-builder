import { runInitSteps } from './init-steps'

vi.mock('../telemetry/logging', () => ({ logError: vi.fn() }))

describe('runInitSteps', () => {
    beforeEach(() => {
        vi.spyOn(console, 'error').mockImplementation(() => {})
    })

    it('runs the steps in order, awaiting async ones', async () => {
        const order: string[] = []

        await runInitSteps([
            [
                'slow',
                async () => {
                    await Promise.resolve()
                    order.push('slow')
                },
            ],
            ['fast', () => void order.push('fast')],
        ])

        expect(order).toEqual(['slow', 'fast'])
    })

    it('keeps going after a step throws', async () => {
        const after = vi.fn()

        await runInitSteps([
            [
                'broken',
                () => {
                    throw new Error('boom')
                },
            ],
            ['after', after],
        ])

        expect(after).toHaveBeenCalledOnce()
        expect(console.error).toHaveBeenCalledOnce()
    })

    it('keeps going after an async step rejects', async () => {
        const after = vi.fn()

        await runInitSteps([
            ['broken', () => Promise.reject(new Error('boom'))],
            ['after', after],
        ])

        expect(after).toHaveBeenCalledOnce()
    })
})
