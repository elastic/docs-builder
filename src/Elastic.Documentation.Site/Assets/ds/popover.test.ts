import { initInlinePopovers, initPopovers } from './popover'

const markup = `
<details data-menu data-popover id="d">
  <summary>Stack 9.1+</summary>
  <div data-popover-panel id="p">Details</div>
</details>`

describe('initPopovers', () => {
    let shown: boolean

    beforeEach(() => {
        shown = false
        document.body.innerHTML = markup
        HTMLElement.prototype.showPopover = function () {
            shown = true
        }
        HTMLElement.prototype.hidePopover = function () {
            shown = false
        }
        HTMLElement.prototype.matches = ((original) =>
            function (this: HTMLElement, selector: string) {
                return selector === ':popover-open'
                    ? shown
                    : original.call(this, selector)
            })(HTMLElement.prototype.matches)
    })

    it('makes the panel a manual popover', () => {
        initPopovers()

        expect(document.getElementById('p')!.getAttribute('popover')).toBe(
            'manual'
        )
    })

    it('shows the panel when the details open and hides it when they close', () => {
        initPopovers()
        const details = document.getElementById('d') as HTMLDetailsElement

        details.open = true
        details.dispatchEvent(new Event('toggle'))
        expect(shown).toBe(true)

        details.open = false
        details.dispatchEvent(new Event('toggle'))
        expect(shown).toBe(false)
    })

    it('positions the panel inside the viewport', () => {
        initPopovers()
        const details = document.getElementById('d') as HTMLDetailsElement
        const panel = document.getElementById('p')!
        details.querySelector('summary')!.getBoundingClientRect = () =>
            ({ left: 5000, bottom: 100, top: 80 }) as DOMRect
        Object.defineProperty(panel, 'offsetWidth', { value: 300 })
        Object.defineProperty(panel, 'offsetHeight', { value: 100 })

        details.open = true
        details.dispatchEvent(new Event('toggle'))

        expect(parseInt(panel.style.left, 10)).toBeLessThanOrEqual(
            window.innerWidth - 300
        )
    })

    it('binds each popover once', () => {
        initPopovers()
        initPopovers()

        expect(document.querySelectorAll('[data-popover-bound]')).toHaveLength(
            1
        )
    })

    it('does nothing when the browser has no popover support', () => {
        // @ts-expect-error simulating a browser without the Popover API
        delete HTMLElement.prototype.showPopover
        initPopovers()

        expect(document.getElementById('p')!.hasAttribute('popover')).toBe(
            false
        )
    })
})

describe('initInlinePopovers', () => {
    const inline = `
<p>Applies to <span class="applies-popover applies-popover--inline" data-applies-popover>
  <button type="button" aria-expanded="false" data-applies-trigger>Stack 9.1+</button>
  <span class="applies-popover__panel">Details</span>
</span> and more.</p>
<button id="outside">outside</button>`

    beforeEach(() => {
        document.body.innerHTML = inline
        initInlinePopovers()
    })

    const root = () =>
        document.querySelector<HTMLElement>('[data-applies-popover]')!
    const trigger = () =>
        document.querySelector<HTMLButtonElement>('[data-applies-trigger]')!

    it('toggles the panel from the button and reports it', () => {
        trigger().click()
        expect(root().hasAttribute('data-open')).toBe(true)
        expect(trigger().getAttribute('aria-expanded')).toBe('true')

        trigger().click()
        expect(root().hasAttribute('data-open')).toBe(false)
        expect(trigger().getAttribute('aria-expanded')).toBe('false')
    })

    it('closes on Escape and returns focus to the button', () => {
        trigger().click()
        root().dispatchEvent(
            new KeyboardEvent('keydown', { key: 'Escape', bubbles: true })
        )

        expect(root().hasAttribute('data-open')).toBe(false)
        expect(document.activeElement).toBe(trigger())
    })

    it('closes when something else is clicked', () => {
        trigger().click()
        document.getElementById('outside')!.click()

        expect(root().hasAttribute('data-open')).toBe(false)
    })

    it('binds each badge once', () => {
        initInlinePopovers()
        trigger().click()

        expect(root().hasAttribute('data-open')).toBe(true)
    })
})
