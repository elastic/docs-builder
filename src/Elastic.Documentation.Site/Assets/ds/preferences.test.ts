import { applyPref, initPreferences, readPref } from './preferences'

function system(contrastMore: boolean, reducedMotion: boolean) {
    window.matchMedia = jest.fn().mockImplementation((query: string) => ({
        matches: query.includes('contrast')
            ? contrastMore
            : query.includes('reduced-motion')
              ? reducedMotion
              : false,
        addEventListener: jest.fn(),
    })) as unknown as typeof window.matchMedia
}

const control = `
<div class="theme-switch">
  <input class="theme-switch__open" type="checkbox">
  <input type="radio" name="t" value="light" data-pref="theme">
  <input type="radio" name="t" value="black" data-pref="theme">
  <input type="radio" name="t" value="system" data-pref="theme">
  <input type="radio" name="c" value="more" data-pref="contrast">
  <input type="radio" name="c" value="system" data-pref="contrast">
  <input type="radio" name="m" value="reduce" data-pref="motion">
  <input type="radio" name="m" value="system" data-pref="motion">
</div>`

describe('preferences', () => {
    beforeEach(() => {
        localStorage.clear()
        for (const key of ['theme', 'contrast', 'motion'])
            delete document.documentElement.dataset[key]
        system(false, false)
        document.body.innerHTML = control
    })

    it('stores a pinned theme, including black, and clears it for system', () => {
        applyPref('theme', 'black')

        expect(document.documentElement.dataset.theme).toBe('black')
        expect(localStorage.getItem('docs-theme')).toBe('black')

        applyPref('theme', 'system')

        expect(document.documentElement.dataset.theme).toBeUndefined()
        expect(localStorage.getItem('docs-theme')).toBeNull()
    })

    it('resolves contrast and motion from the device for system', () => {
        system(true, true)

        applyPref('contrast', 'system')
        applyPref('motion', 'system')

        expect(document.documentElement.dataset.contrast).toBe('more')
        expect(document.documentElement.dataset.motion).toBe('reduce')
    })

    it('ignores a stored value that is not a choice', () => {
        localStorage.setItem('docs-theme', 'sepia')

        expect(readPref('theme')).toBe('system')
    })

    it('shows the saved choices in the control and applies a change', () => {
        localStorage.setItem('docs-theme', 'light')
        initPreferences()
        const radios = (value: string, pref: string) =>
            document.querySelector<HTMLInputElement>(
                `input[data-pref="${pref}"][value="${value}"]`
            )!

        expect(radios('light', 'theme').checked).toBe(true)
        expect(radios('system', 'contrast').checked).toBe(true)

        radios('black', 'theme').checked = true
        radios('black', 'theme').dispatchEvent(new Event('change'))

        expect(document.documentElement.dataset.theme).toBe('black')
    })

    it('closes the menu on Escape and on a click outside', () => {
        initPreferences()
        const open = document.querySelector<HTMLInputElement>(
            '.theme-switch__open'
        )!

        open.checked = true
        document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }))
        expect(open.checked).toBe(false)

        open.checked = true
        document.body.click()
        expect(open.checked).toBe(false)
    })
})
