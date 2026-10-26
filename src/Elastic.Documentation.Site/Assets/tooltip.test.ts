import { flashTooltip, initTooltips } from './tooltip'

const roots = () => document.querySelectorAll('[data-tippy-root]')
const tippyOf = (el: HTMLElement) =>
    (el as HTMLElement & { _tippy?: unknown })._tippy

function hover(id: string) {
    document
        .getElementById(id)!
        .dispatchEvent(new MouseEvent('mouseover', { bubbles: true }))
    jest.advanceTimersByTime(80)
}

function swap() {
    document.body.dispatchEvent(
        new CustomEvent('htmx:afterSwap', { bubbles: true })
    )
}

describe('initTooltips', () => {
    let t: HTMLElement

    beforeEach(() => {
        jest.useFakeTimers()
        document.body.innerHTML =
            '<div id="page"><span id="t" data-tippy-content="hits.total">x</span></div>'
        t = document.getElementById('t')!
        initTooltips()
        hover('t')
        expect(roots().length).toBe(1)
        expect(roots()[0].querySelector('.tippy-content')!.textContent).toBe(
            'hits.total'
        )
    })

    afterEach(() => {
        swap()
        jest.useRealTimers()
    })

    it('destroys the tooltips of swapped-out content', () => {
        document.getElementById('page')!.remove()
        swap()

        expect(roots().length).toBe(0)
        expect(tippyOf(t)).toBeUndefined()
    })

    it('releases a tooltip that was shown and hidden before the swap', () => {
        t.dispatchEvent(new MouseEvent('mouseleave'))
        jest.advanceTimersByTime(20)
        // jsdom runs no CSS transitions, so the fade-out that unmounts the tooltip has to be ended by hand
        roots()[0]
            .querySelector('.tippy-box')!
            .dispatchEvent(new Event('transitionend'))
        expect(roots().length).toBe(0)

        document.getElementById('page')!.remove()
        swap()

        expect(tippyOf(t)).toBeUndefined()
    })

    it('installs tooltips again for the content that replaced it', () => {
        document.getElementById('page')!.remove()
        swap()
        document.body.insertAdjacentHTML(
            'beforeend',
            '<div id="page"><span id="u" data-tippy-content="took">y</span></div>'
        )

        initTooltips()
        hover('u')

        expect(roots().length).toBe(1)
        expect(roots()[0].querySelector('.tippy-content')!.textContent).toBe(
            'took'
        )
    })
})

describe('flashTooltip', () => {
    beforeEach(() => jest.useFakeTimers())
    afterEach(() => jest.useRealTimers())

    it('swaps the text and restores it after the delay', () => {
        const el = document.createElement('button')
        el.dataset.tippyContent = 'Copy'
        flashTooltip(el, 'Copied!')
        expect(el.dataset.tippyContent).toBe('Copied!')
        jest.advanceTimersByTime(1500)
        expect(el.dataset.tippyContent).toBe('Copy')
    })

    it('keeps the original text across repeated flashes', () => {
        const el = document.createElement('button')
        el.dataset.tippyContent = 'Copy'
        flashTooltip(el, 'Copied!')
        flashTooltip(el, 'Copied!')
        jest.advanceTimersByTime(1500)
        expect(el.dataset.tippyContent).toBe('Copy')
    })

    it('restarts the delay on a repeated flash', () => {
        const el = document.createElement('button')
        el.dataset.tippyContent = 'Copy'
        flashTooltip(el, 'Copied!')
        jest.advanceTimersByTime(1000)
        flashTooltip(el, 'Copied!')
        jest.advanceTimersByTime(1000)
        expect(el.dataset.tippyContent).toBe('Copied!')
        jest.advanceTimersByTime(500)
        expect(el.dataset.tippyContent).toBe('Copy')
    })
})
