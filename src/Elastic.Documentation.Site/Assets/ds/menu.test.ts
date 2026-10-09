import { bindOutsideClick, initMenus } from './menu'

const menu = `
<details data-menu id="m"><summary>Version</summary><ul><li><a href="#x">9.0</a></li></ul></details>
<p id="outside">outside</p>`

describe('menus', () => {
    beforeEach(() => {
        document.body.innerHTML = menu
        initMenus()
        bindOutsideClick()
    })

    it('closes on Escape', () => {
        const details = document.querySelector<HTMLDetailsElement>('#m')!
        details.open = true
        details.dispatchEvent(
            new KeyboardEvent('keydown', { key: 'Escape', bubbles: true })
        )

        expect(details.open).toBe(false)
    })

    it('closes after choosing a link', () => {
        const details = document.querySelector<HTMLDetailsElement>('#m')!
        details.open = true
        details
            .querySelector('a')!
            .dispatchEvent(new MouseEvent('click', { bubbles: true }))

        expect(details.open).toBe(false)
    })

    it('closes on a click outside', () => {
        const details = document.querySelector<HTMLDetailsElement>('#m')!
        details.open = true
        document
            .getElementById('outside')!
            .dispatchEvent(new MouseEvent('click', { bubbles: true }))

        expect(details.open).toBe(false)
    })

    it('stays open for a click inside the list', () => {
        const details = document.querySelector<HTMLDetailsElement>('#m')!
        details.open = true
        details
            .querySelector('ul')!
            .dispatchEvent(new MouseEvent('click', { bubbles: true }))

        expect(details.open).toBe(true)
    })
})
