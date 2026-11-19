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
        const added = vi.spyOn(document, 'addEventListener')
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

describe('mermaid fullscreen', () => {
    const modalViewport = () =>
        document.querySelector('.mermaid-modal-viewport') as HTMLElement
    const modalRendered = () =>
        modalViewport().querySelector('.mermaid-rendered') as HTMLElement
    const dialog = () =>
        document.querySelector<HTMLDialogElement>('dialog.mermaid-modal')

    beforeAll(() => {
        // jsdom has no modal dialogs; these do what the browser does to the attribute and events.
        HTMLDialogElement.prototype.showModal = function (
            this: HTMLDialogElement
        ) {
            this.setAttribute('open', '')
        }
        HTMLDialogElement.prototype.close = function (this: HTMLDialogElement) {
            this.removeAttribute('open')
            this.dispatchEvent(new Event('close'))
        }
    })

    function openFullscreen(name: string) {
        document
            .querySelector<HTMLButtonElement>(`#${name} .mermaid-fullscreen`)!
            .click()
    }

    afterEach(() => {
        // A test that leaves the dialog open leaves the page scroll locked.
        document.documentElement.removeAttribute('style')
    })

    it('pans the fullscreen diagram without adding document listeners', () => {
        document.body.innerHTML = diagram('a')
        initMermaid()
        const added = vi.spyOn(document, 'addEventListener')

        openFullscreen('a')
        press(modalViewport(), 10, 10)
        move(40, 25)
        release()
        move(90, 90)

        expect(modalRendered().style.transform).toBe(
            'scale(1) translate(30px, 15px)'
        )
        expect(rendered('a').style.transform).toBe('')
        expect(
            added.mock.calls
                .map(([type]) => type)
                .filter((type) => type === 'mousemove' || type === 'mouseup')
        ).toEqual([])
    })

    it('opens the diagram in a modal dialog that locks the page scroll', () => {
        document.body.innerHTML = diagram('a')
        initMermaid()
        const showModal = vi.spyOn(HTMLDialogElement.prototype, 'showModal')

        openFullscreen('a')

        expect(showModal).toHaveBeenCalledTimes(1)
        expect(dialog()?.open).toBe(true)
        expect(modalRendered().querySelector('img')?.src).toContain('a.svg')
        expect(document.documentElement.style.overflow).toBe('hidden')
    })

    it('closes on the close button', () => {
        document.body.innerHTML = diagram('a')
        initMermaid()
        openFullscreen('a')
        expect(dialog()?.open).toBe(true)

        document
            .querySelector<HTMLButtonElement>('.mermaid-modal-close')!
            .click()

        expect(dialog()).toBeNull()
        expect(document.documentElement.style.overflow).toBe('')
    })

    it('closes on a click on the backdrop, and stays open for a click inside', () => {
        document.body.innerHTML = diagram('a')
        initMermaid()
        openFullscreen('a')

        modalViewport().dispatchEvent(
            new MouseEvent('click', { bubbles: true })
        )
        expect(dialog()?.open).toBe(true)

        dialog()!.dispatchEvent(new MouseEvent('click', { bubbles: true }))
        expect(dialog()).toBeNull()
    })

    it('cleans up when the browser closes it, as it does on Escape', () => {
        document.body.innerHTML = diagram('a')
        initMermaid()
        openFullscreen('a')

        dialog()!.close()

        expect(dialog()).toBeNull()
        expect(document.documentElement.style.overflow).toBe('')
    })
})
