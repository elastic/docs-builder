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

describe('hover menus', () => {
    beforeEach(() => {
        jest.useFakeTimers()
        document.body.innerHTML =
            '<details data-menu data-hover-menu id="h"><summary>main</summary><div>info</div></details>'
        initMenus()
    })
    afterEach(() => jest.useRealTimers())

    const pointer = (type: string, pointerType: string) => {
        const event = new Event(type, { bubbles: false })
        Object.defineProperty(event, 'pointerType', { value: pointerType })
        return event
    }

    it('opens when a mouse rests on it and closes shortly after it leaves', () => {
        const details = document.querySelector<HTMLDetailsElement>('#h')!

        details.dispatchEvent(pointer('pointerenter', 'mouse'))
        expect(details.open).toBe(true)

        details.dispatchEvent(pointer('pointerleave', 'mouse'))
        expect(details.open).toBe(true)
        jest.advanceTimersByTime(200)
        expect(details.open).toBe(false)
    })

    it('ignores touch, which keeps using the click', () => {
        const details = document.querySelector<HTMLDetailsElement>('#h')!

        details.dispatchEvent(pointer('pointerenter', 'touch'))

        expect(details.open).toBe(false)
    })
})
