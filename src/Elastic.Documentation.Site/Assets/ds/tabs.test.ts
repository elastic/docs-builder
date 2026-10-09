import { initTabSync } from './tabs'

function setup(html: string) {
    document.body.innerHTML = html
}

const tabs = (setIndex: number, active: number) => `
    <div class="tab-set">
        <input type="radio" name="s${setIndex}" id="s${setIndex}-0" ${active === 0 ? 'checked' : ''}>
        <label for="s${setIndex}-0" data-tab-sync-group="lang" data-tab-sync-key="curl">curl</label>
        <div></div>
        <input type="radio" name="s${setIndex}" id="s${setIndex}-1" ${active === 1 ? 'checked' : ''}>
        <label for="s${setIndex}-1" data-tab-sync-group="lang" data-tab-sync-key="python">python</label>
        <div></div>
    </div>`

describe('initTabSync', () => {
    beforeEach(() => {
        window.sessionStorage.clear()
        window.history.pushState({}, '', '/')
    })

    it('selects the same tab in every set when one is clicked', () => {
        setup(tabs(1, 0) + tabs(2, 0))
        initTabSync()

        document.querySelector<HTMLElement>('#s1-1 + label')!.click()

        expect(document.querySelector<HTMLInputElement>('#s2-1')!.checked).toBe(
            true
        )
        expect(window.sessionStorage.getItem('tab-id-lang')).toBe('python')
    })

    it('restores the stored selection on load', () => {
        window.sessionStorage.setItem('tab-id-lang', 'python')
        setup(tabs(1, 0))

        initTabSync()

        expect(document.querySelector<HTMLInputElement>('#s1-1')!.checked).toBe(
            true
        )
    })

    it('lets a query parameter named after the group preselect a tab', () => {
        window.history.pushState({}, '', '/?lang=python')
        setup(tabs(1, 0))

        initTabSync()

        expect(document.querySelector<HTMLInputElement>('#s1-1')!.checked).toBe(
            true
        )
    })

    it('binds each label once', () => {
        setup(tabs(1, 0))
        initTabSync()
        initTabSync()

        document.querySelector<HTMLElement>('#s1-1 + label')!.click()

        expect(window.sessionStorage.getItem('tab-id-lang')).toBe('python')
    })

    it('leaves tabs without sync attributes alone', () => {
        setup(
            '<div class="tab-set"><input type="radio" id="a" checked><label for="a">a</label></div>'
        )

        expect(() => initTabSync()).not.toThrow()
    })
})
