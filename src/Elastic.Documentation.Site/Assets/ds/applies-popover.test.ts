import { initAppliesPopovers, unbindAppliesPopovers } from './applies-popover'

const badge = `
<span class="applies-popover" data-applies-popover>
  <button type="button" aria-expanded="false" data-applies-trigger>Stack 9.1+</button>
  <span data-applies-panel>Details</span>
</span>`

describe('applies-to popovers', () => {
    const showPopover = HTMLElement.prototype.showPopover

    beforeEach(() => {
        // jsdom has no popover API; the module binds only where it exists.
        HTMLElement.prototype.showPopover = vi.fn()
        document.body.innerHTML = badge + badge
    })

    afterEach(() => {
        HTMLElement.prototype.showPopover = showPopover
    })

    const triggers = () =>
        Array.from(document.querySelectorAll('[data-applies-trigger]'))
    const panels = () =>
        Array.from(document.querySelectorAll('[data-applies-panel]'))

    it('makes each button open its own panel as a popover', () => {
        initAppliesPopovers()

        const [first, second] = panels()
        expect(first.getAttribute('popover')).toBe('')
        expect(first.id).not.toBe(second.id)
        triggers().forEach((trigger, i) =>
            expect(trigger.getAttribute('popovertarget')).toBe(panels()[i].id)
        )
    })

    it('gives badges brought in by a later swap ids of their own', () => {
        initAppliesPopovers()
        const before = panels().map((panel) => panel.id)

        document.body.insertAdjacentHTML('beforeend', badge)
        initAppliesPopovers()

        const added = panels()[2]
        expect(before).not.toContain(added.id)
        expect(
            panels()
                .slice(0, 2)
                .map((panel) => panel.id)
        ).toEqual(before)
    })

    it('leaves the panel to show on focus where the browser has no popovers', () => {
        HTMLElement.prototype.showPopover =
            undefined as unknown as typeof showPopover

        initAppliesPopovers()

        expect(panels()[0].hasAttribute('popover')).toBe(false)
        expect(
            document
                .querySelector('[data-applies-popover]')!
                .hasAttribute('data-applies-bound')
        ).toBe(false)
    })

    it('gives a cloned badge a panel of its own once it is unbound', () => {
        document.body.innerHTML = badge
        initAppliesPopovers()
        const original = panels()[0]
        triggers()[0].setAttribute('aria-expanded', 'true')
        const copy = document.createElement('div')
        copy.innerHTML = document.body.innerHTML

        unbindAppliesPopovers(copy)
        document.body.append(copy)
        initAppliesPopovers(copy)

        const copied = copy.querySelector('[data-applies-panel]')!
        expect(copied.id).not.toBe(original.id)
        expect(
            copy
                .querySelector('[data-applies-trigger]')!
                .getAttribute('popovertarget')
        ).toBe(copied.id)
        expect(
            copy
                .querySelector('[data-applies-trigger]')!
                .getAttribute('aria-expanded')
        ).toBe('false')
    })
})
