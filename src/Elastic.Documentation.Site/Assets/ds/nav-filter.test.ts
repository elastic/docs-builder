import { initNavFilter, matchesFilter } from './nav-filter'

const markup = `
<div data-nav-filter-scope>
  <input data-nav-filter hidden>
  <ul class="nav-tree">
    <li class="nav-tree__item"><a class="nav-tree__link" href="/a">Search</a></li>
    <li class="nav-tree__group">
      <input type="checkbox" data-nav-toggle>
      <div class="nav-tree__row"><a class="nav-tree__link" href="/b">Indices</a></div>
      <ul class="nav-tree__children">
        <li class="nav-tree__item"><a class="nav-tree__link" href="/b/c">Create index</a></li>
        <li class="nav-tree__item"><a class="nav-tree__link" href="/b/d">Delete index</a></li>
      </ul>
    </li>
  </ul>
</div>`

const type = (value: string) => {
    const input = document.querySelector<HTMLInputElement>('[data-nav-filter]')!
    input.value = value
    input.dispatchEvent(new Event('input'))
}
const hiddenLinks = () =>
    Array.from(document.querySelectorAll<HTMLElement>('li'))
        .filter((li) => li.hidden)
        .map((li) => li.querySelector('a')!.textContent)

describe('nav filter', () => {
    beforeEach(() => {
        document.body.innerHTML = markup
        initNavFilter()
    })

    it('reveals the field', () => {
        expect(
            document.querySelector<HTMLElement>('[data-nav-filter]')!.hidden
        ).toBe(false)
    })

    it('matches without regard to case or surrounding spaces', () => {
        expect(matchesFilter('Create index', ' CREATE ')).toBe(true)
        expect(matchesFilter('Create index', 'delete')).toBe(false)
    })

    it('hides what does not match and opens the folder that holds a match', () => {
        type('create')

        expect(hiddenLinks()).toEqual(['Search', 'Delete index'])
        expect(
            document.querySelector<HTMLInputElement>('[data-nav-toggle]')!
                .checked
        ).toBe(true)
    })

    it('shows everything again when cleared', () => {
        type('create')
        type('')

        expect(hiddenLinks()).toEqual([])
    })
})
