import { initTabs } from './tabs'

/**
 * Mirrors the markup emitted by TabSetView.cshtml / TabItemView.cshtml.
 */
function tabSet(
    index: number,
    options: { title: string; sync?: string }[],
    { group, dropdown }: { group?: string; dropdown?: boolean } = {}
): string {
    const inputId = (i: number) => `tabs-item-${index}-${i}`
    const select = dropdown
        ? `<div class="tabs-select-wrapper" hidden>
               <select class="tabs-select" data-sync-group="${group ?? ''}">
                   ${options
                       .map(
                           (o, i) =>
                               `<option value="${inputId(i)}" data-sync-id="${o.sync ?? ''}">${o.title}</option>`
                       )
                       .join('')}
               </select>
           </div>`
        : ''
    const items = options
        .map(
            (o, i) =>
                `<input class="tabs-input" ${i === 0 ? 'checked' : ''} id="${inputId(i)}" name="tabs-set-${index}" type="radio">
                 <label class="tabs-label" data-sync-id="${o.sync ?? ''}" data-sync-group="${group ?? ''}" for="${inputId(i)}">${o.title}</label>
                 <div class="tabs-content"></div>`
        )
        .join('')
    return `<div class="tabs ${dropdown ? 'tabs-dropdown' : ''}">${select}${items}</div>`
}

const LANGUAGES = [
    { title: 'Java', sync: 'java' },
    { title: 'Golang', sync: 'golang' },
    { title: 'C#', sync: 'csharp' },
]

const selects = () =>
    Array.from(document.querySelectorAll<HTMLSelectElement>('.tabs-select'))
const checkedIn = (tabsIndex: number) => {
    const set = document.querySelectorAll<HTMLElement>('.tabs')[tabsIndex]
    return set.querySelector<HTMLInputElement>('.tabs-input:checked')!.id
}

beforeEach(() => {
    window.sessionStorage.clear()
    document.body.innerHTML = ''
})

describe('dropdown tab sets', () => {
    it('reveals the select and marks the tab set as dropdown-driven', () => {
        document.body.innerHTML = tabSet(1, LANGUAGES, {
            group: 'languages',
            dropdown: true,
        })
        const wrapper = document.querySelector<HTMLElement>(
            '.tabs-select-wrapper'
        )!
        expect(wrapper.hidden).toBe(true)

        initTabs()

        expect(wrapper.hidden).toBe(false)
        expect(
            document
                .querySelector('.tabs')!
                .classList.contains('tabs-dropdown-active')
        ).toBe(true)
    })

    it('checks the radio input the selected option points at', () => {
        document.body.innerHTML = tabSet(1, LANGUAGES, {
            group: 'languages',
            dropdown: true,
        })
        initTabs()

        const select = selects()[0]
        select.value = 'tabs-item-1-1'
        select.dispatchEvent(new Event('change'))

        expect(checkedIn(0)).toBe('tabs-item-1-1')
    })

    it('syncs another dropdown in the same group', () => {
        document.body.innerHTML =
            tabSet(1, LANGUAGES, { group: 'languages', dropdown: true }) +
            tabSet(2, LANGUAGES, { group: 'languages', dropdown: true })
        initTabs()

        const [first, second] = selects()
        first.value = 'tabs-item-1-2'
        first.dispatchEvent(new Event('change'))

        expect(checkedIn(1)).toBe('tabs-item-2-2')
        expect(second.value).toBe('tabs-item-2-2')
        expect(window.sessionStorage.getItem('tab-id-languages')).toBe('csharp')
    })

    it('syncs with a tab set in the same group still rendered as a strip', () => {
        document.body.innerHTML =
            tabSet(1, LANGUAGES, { group: 'languages', dropdown: true }) +
            tabSet(2, LANGUAGES, { group: 'languages' })
        initTabs()

        const select = selects()[0]
        select.value = 'tabs-item-1-1'
        select.dispatchEvent(new Event('change'))
        expect(checkedIn(1)).toBe('tabs-item-2-1')

        // ...and the other way around: clicking a label updates the dropdown.
        const label = document.querySelectorAll<HTMLLabelElement>(
            '.tabs-label[for="tabs-item-2-2"]'
        )[0]
        label.click()
        expect(checkedIn(0)).toBe('tabs-item-1-2')
        expect(select.value).toBe('tabs-item-1-2')
    })

    it('restores the select from a previous session selection', () => {
        window.sessionStorage.setItem('tab-id-languages', 'golang')
        document.body.innerHTML = tabSet(1, LANGUAGES, {
            group: 'languages',
            dropdown: true,
        })
        initTabs()

        expect(checkedIn(0)).toBe('tabs-item-1-1')
        expect(selects()[0].value).toBe('tabs-item-1-1')
    })

    it('leaves ungrouped tab sets as a plain tab strip', () => {
        document.body.innerHTML = tabSet(1, [
            { title: 'One' },
            { title: 'Two' },
        ])
        initTabs()

        expect(selects()).toHaveLength(0)
        expect(
            document
                .querySelector('.tabs')!
                .classList.contains('tabs-dropdown-active')
        ).toBe(false)
    })
})
