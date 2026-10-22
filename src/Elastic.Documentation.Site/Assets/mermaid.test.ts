import { initMermaid } from './mermaid'

function diagram(name: string): string {
    return `
        <div class="mermaid-container" id="${name}">
            <div class="mermaid-viewport">
                <div class="mermaid-rendered" data-src="${name}.svg">
                    <img src="${name}.svg" alt="Mermaid diagram">
                </div>
            </div>
        </div>`
}

const viewport = (name: string) =>
    document.querySelector(`#${name} .mermaid-viewport`) as HTMLElement
const rendered = (name: string) =>
    document.querySelector(`#${name} .mermaid-rendered`) as HTMLElement

function press(target: HTMLElement, clientX: number, clientY: number) {
    target.dispatchEvent(
        new MouseEvent('mousedown', {
            bubbles: true,
            button: 0,
            clientX,
            clientY,
        })
    )
}

function move(clientX: number, clientY: number) {
    document.dispatchEvent(
        new MouseEvent('mousemove', { bubbles: true, clientX, clientY })
    )
}

function release() {
    document.dispatchEvent(new MouseEvent('mouseup', { bubbles: true }))
}

describe('mermaid pan', () => {
    afterEach(() => jest.restoreAllMocks())

    it('pans the dragged diagram', () => {
        document.body.innerHTML = diagram('a')
        initMermaid()

        press(viewport('a'), 10, 10)
        expect(viewport('a').classList.contains('is-dragging')).toBe(true)
        move(40, 25)
        expect(rendered('a').style.transform).toBe(
            'scale(1) translate(30px, 15px)'
        )

        release()
        move(90, 90)
        expect(viewport('a').classList.contains('is-dragging')).toBe(false)
        expect(rendered('a').style.transform).toBe(
            'scale(1) translate(30px, 15px)'
        )
    })

    it('pans only the diagram under the pointer', () => {
        document.body.innerHTML = diagram('a') + diagram('b')
        initMermaid()

        press(viewport('a'), 10, 10)
        move(40, 25)
        release()

        expect(rendered('a').style.transform).toBe(
            'scale(1) translate(30px, 15px)'
        )
        expect(rendered('b').style.transform).toBe('')
    })

    it('keeps panning the diagram of the page that replaced the old one', () => {
        document.body.innerHTML = diagram('a')
        initMermaid()
        const old = rendered('a')

        document.body.innerHTML = diagram('b')
        initMermaid()
        press(viewport('b'), 10, 10)
        move(40, 25)
        release()

        expect(rendered('b').style.transform).toBe(
            'scale(1) translate(30px, 15px)'
        )
        expect(old.style.transform).toBe('')
    })

    it('adds no document listeners when pages with diagrams are swapped', () => {
        const added = jest.spyOn(document, 'addEventListener')
        document.body.innerHTML = diagram('a')
        initMermaid()
        document.body.innerHTML = diagram('b')
        initMermaid()

        press(viewport('b'), 10, 10)
        move(40, 25)
        release()

        // The pan proves both pages were initialized, so an empty list below is not a no-op passing.
        expect(rendered('b').style.transform).toBe(
            'scale(1) translate(30px, 15px)'
        )
        expect(
            added.mock.calls
                .map(([type]) => type)
                .filter((type) => type === 'mousemove' || type === 'mouseup')
        ).toEqual([])
    })
})
