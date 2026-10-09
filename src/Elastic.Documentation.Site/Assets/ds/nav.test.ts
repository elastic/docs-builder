import { initNav, markCurrent } from './nav'

const tree = `
<div data-nav-side>
  <nav class="nav-side__scroll">
    <ul class="nav-tree">
      <li class="nav-tree__item"><a class="nav-tree__link" href="/docs/start/">Start</a></li>
      <li class="nav-tree__group">
        <input type="checkbox" id="g1" data-nav-toggle>
        <div class="nav-tree__row"><a class="nav-tree__link" href="/docs/guide">Guide</a></div>
        <ul class="nav-tree__children">
          <li class="nav-tree__group">
            <input type="checkbox" id="g2" data-nav-toggle>
            <div class="nav-tree__row"><a class="nav-tree__link" href="/docs/guide/deep">Deep</a></div>
            <ul class="nav-tree__children">
              <li class="nav-tree__item"><a class="nav-tree__link" href="/docs/guide/deep/page">Page</a></li>
            </ul>
          </li>
        </ul>
      </li>
    </ul>
  </nav>
</div>`

function goTo(path: string) {
    window.history.pushState({}, '', path)
}

describe('initNav', () => {
    beforeEach(() => {
        window.sessionStorage.clear()
        document.head.innerHTML = ''
        document.body.innerHTML = tree
    })

    it('marks the current page and opens every ancestor folder', () => {
        goTo('/docs/guide/deep/page')
        initNav()

        const current = document.querySelector('[aria-current="page"]')!
        expect(current.textContent).toBe('Page')
        expect(document.querySelector<HTMLInputElement>('#g1')!.checked).toBe(
            true
        )
        expect(document.querySelector<HTMLInputElement>('#g2')!.checked).toBe(
            true
        )
    })

    it('ignores a trailing slash when matching', () => {
        goTo('/docs/start')
        initNav()

        expect(
            document.querySelector('[aria-current="page"]')!.textContent
        ).toBe('Start')
    })

    it('prefers the active url from the page meta tag', () => {
        document.head.innerHTML =
            '<meta name="docs:nav-active" content="/docs/guide">'
        goTo('/somewhere/else')
        initNav()

        expect(
            document.querySelector('[aria-current="page"]')!.textContent
        ).toBe('Guide')
    })

    it('restores and persists folder state', () => {
        goTo('/docs/start')
        window.sessionStorage.setItem('nav-open:g1', '1')
        initNav()

        const g1 = document.querySelector<HTMLInputElement>('#g1')!
        expect(g1.checked).toBe(true)

        g1.checked = false
        g1.dispatchEvent(new Event('change'))
        expect(window.sessionStorage.getItem('nav-open:g1')).toBe('0')
    })

    it('moves the current marker when navigating inside the same tree', () => {
        goTo('/docs/start')
        initNav()
        goTo('/docs/guide')
        markCurrent(document)

        expect(document.querySelectorAll('[aria-current="page"]')).toHaveLength(
            1
        )
        expect(
            document.querySelector('[aria-current="page"]')!.textContent
        ).toBe('Guide')
    })

    it('does nothing when no link matches', () => {
        goTo('/docs/missing')

        expect(() => initNav()).not.toThrow()
        expect(document.querySelector('[aria-current="page"]')).toBeNull()
    })
})
