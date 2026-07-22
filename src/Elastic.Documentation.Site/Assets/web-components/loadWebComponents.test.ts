import { createWebComponentLoader } from './loadWebComponents'

describe('web component loader', () => {
    beforeEach(() => {
        document.body.innerHTML = ''
    })

    it('does not load a component when its host is absent', async () => {
        const load = jest.fn().mockResolvedValue(undefined)
        const loadWebComponents = createWebComponentLoader({
            'absent-component': load,
        })

        await loadWebComponents()

        expect(load).not.toHaveBeenCalled()
    })

    it('loads a component when its host is present', async () => {
        const load = jest.fn().mockImplementation(async () => {
            customElements.define(
                'present-component',
                class extends HTMLElement {}
            )
        })
        const loadWebComponents = createWebComponentLoader({
            'present-component': load,
        })
        document.body.innerHTML = '<present-component></present-component>'

        await loadWebComponents()

        expect(load).toHaveBeenCalledTimes(1)
        expect(customElements.get('present-component')).toBeDefined()
    })

    it('loads a component introduced before a subsequent scan', async () => {
        const load = jest.fn().mockResolvedValue(undefined)
        const loadWebComponents = createWebComponentLoader({
            'swapped-component': load,
        })

        await loadWebComponents()
        document.body.innerHTML = '<swapped-component></swapped-component>'
        await loadWebComponents()

        expect(load).toHaveBeenCalledTimes(1)
    })

    it('deduplicates concurrent and repeated loads', async () => {
        let resolveLoad: () => void
        const pendingLoad = new Promise<void>((resolve) => {
            resolveLoad = resolve
        })
        const load = jest.fn(() => pendingLoad)
        const loadWebComponents = createWebComponentLoader({
            'deduplicated-component': load,
        })
        document.body.innerHTML =
            '<deduplicated-component></deduplicated-component>'

        const firstLoad = loadWebComponents()
        const secondLoad = loadWebComponents()
        resolveLoad!()
        await Promise.all([firstLoad, secondLoad])
        await loadWebComponents()

        expect(load).toHaveBeenCalledTimes(1)
    })

    it('skips a component that is already registered', async () => {
        customElements.define(
            'registered-component',
            class extends HTMLElement {}
        )
        const load = jest.fn().mockResolvedValue(undefined)
        const loadWebComponents = createWebComponentLoader({
            'registered-component': load,
        })
        document.body.innerHTML =
            '<registered-component></registered-component>'

        await loadWebComponents()

        expect(load).not.toHaveBeenCalled()
    })

    it('retries a component after its load fails', async () => {
        const load = jest
            .fn()
            .mockRejectedValueOnce(new Error('load failed'))
            .mockResolvedValueOnce(undefined)
        const loadWebComponents = createWebComponentLoader({
            'retry-component': load,
        })
        document.body.innerHTML = '<retry-component></retry-component>'

        await expect(loadWebComponents()).rejects.toThrow('load failed')
        await expect(loadWebComponents()).resolves.toBeUndefined()

        expect(load).toHaveBeenCalledTimes(2)
    })
})
