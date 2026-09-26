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
                `<input class="tabs-input" ${i === 0 ? 'checked' : ''} id="${inputId(i)}" name="tabs-set-${index}" type="radio" tabindex="0">
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

describe('keyboard reachability of the fallback radios', () => {
    const radios = () =>
        Array.from(document.querySelectorAll<HTMLInputElement>('.tabs-input'))

    it('takes the hidden radios out of the tab order in dropdown mode', () => {
        document.body.innerHTML = tabSet(1, LANGUAGES, {
            group: 'languages',
            dropdown: true,
        })
        expect(radios().every((r) => r.tabIndex === 0)).toBe(true)

        initTabs()

        // The tab strip is hidden, so reaching a radio would flip the panel
        // without the <select> knowing about it.
        expect(radios().every((r) => r.tabIndex === -1)).toBe(true)
        expect(
            radios().every((r) => r.getAttribute('aria-hidden') === 'true')
        ).toBe(true)
    })

    it('leaves the radios reachable when the tab strip is still visible', () => {
        document.body.innerHTML = tabSet(1, LANGUAGES, { group: 'languages' })
        initTabs()

        expect(radios().every((r) => r.tabIndex === 0)).toBe(true)
        expect(radios().some((r) => r.hasAttribute('aria-hidden'))).toBe(false)
    })
})

describe('dropdown tab sets containing a nested tab set', () => {
    // Outer tab 2 is the restored one; outer tab 1 holds a nested tab set whose
    // own first radio is checked and therefore appears earlier in the document.
    function withNested(): string {
        const nested = tabSet(
            9,
            [
                { title: 'Query DSL', sync: 'dsl' },
                { title: 'ES|QL', sync: 'esql' },
            ],
            { group: 'query-language' }
        )
        const id = (i: number) => `tabs-item-1-${i}`
        const options = [
            { title: 'Python', sync: 'python' },
            { title: 'Java', sync: 'java' },
        ]
        const select = `<div class="tabs-select-wrapper" hidden>
               <select class="tabs-select" data-sync-group="languages">
                   ${options.map((o, i) => `<option value="${id(i)}" data-sync-id="${o.sync}">${o.title}</option>`).join('')}
               </select>
           </div>`
        const items = options
            .map(
                (o, i) =>
                    `<input class="tabs-input" ${i === 1 ? 'checked' : ''} id="${id(i)}" name="tabs-set-1" type="radio" tabindex="0">
                     <label class="tabs-label" data-sync-id="${o.sync}" data-sync-group="languages" for="${id(i)}">${o.title}</label>
                     <div class="tabs-content">${i === 0 ? nested : ''}</div>`
            )
            .join('')
        return `<div class="tabs tabs-dropdown">${select}${items}</div>`
    }

    it('reads its own checked radio, not a nested one', () => {
        document.body.innerHTML = withNested()
        initTabs()

        const select = document.querySelector<HTMLSelectElement>(
            '.tabs-dropdown > .tabs-select-wrapper > .tabs-select'
        )!
        expect(select.value).toBe('tabs-item-1-1')
        expect(select.selectedOptions[0].textContent).toBe('Java')
    })

    it('leaves the nested tab set its own radios', () => {
        document.body.innerHTML = withNested()
        initTabs()

        const nestedRadios = Array.from(
            document.querySelectorAll<HTMLInputElement>(
                '.tabs-content .tabs-input'
            )
        )
        expect(nestedRadios.length).toBeGreaterThan(0)
        expect(nestedRadios.every((r) => r.tabIndex === 0)).toBe(true)
    })
})
