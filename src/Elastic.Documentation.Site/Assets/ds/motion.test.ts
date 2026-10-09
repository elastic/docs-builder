import {
    applyMotion,
    initMotionToggles,
    motionStorageKey,
    nextMotion,
    readMotion,
} from './motion'

function system(reduced: boolean) {
    window.matchMedia = jest.fn().mockReturnValue({
        matches: reduced,
        addEventListener: jest.fn(),
    }) as unknown as typeof window.matchMedia
}

describe('motion', () => {
    beforeEach(() => {
        localStorage.clear()
        delete document.documentElement.dataset.motion
        document.body.innerHTML =
            '<button data-motion-toggle hidden><span data-motion-label>System</span></button>'
    })

    it('cycles system, reduced, full', () => {
        expect(nextMotion('system')).toBe('reduce')
        expect(nextMotion('reduce')).toBe('full')
        expect(nextMotion('full')).toBe('system')
    })

    it('resolves the system choice from the operating system setting', () => {
        system(true)
        applyMotion('system')
        expect(document.documentElement.dataset.motion).toBe('reduce')

        system(false)
        applyMotion('system')
        expect(document.documentElement.dataset.motion).toBe('full')
    })

    it('lets a reader force full motion over a reduced system and remembers it', () => {
        system(true)
        applyMotion('full')

        expect(document.documentElement.dataset.motion).toBe('full')
        expect(localStorage.getItem(motionStorageKey)).toBe('full')
        expect(readMotion()).toBe('full')
    })

    it('forgets the choice when it goes back to the system', () => {
        system(false)
        applyMotion('reduce')
        applyMotion('system')

        expect(localStorage.getItem(motionStorageKey)).toBeNull()
    })

    it('reveals a hidden toggle, names the choice, and cycles on click', () => {
        system(true)
        initMotionToggles()
        const button = document.querySelector<HTMLButtonElement>(
            '[data-motion-toggle]'
        )!
        expect(button.hidden).toBe(false)

        button.click()
        expect(document.documentElement.dataset.motion).toBe('reduce')
        expect(button.querySelector('[data-motion-label]')!.textContent).toBe(
            'Reduced'
        )

        button.click()
        expect(document.documentElement.dataset.motion).toBe('full')
        expect(button.querySelector('[data-motion-label]')!.textContent).toBe(
            'Full'
        )
        expect(button.getAttribute('aria-label')).toContain('full')
    })
})
