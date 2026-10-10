import { initPageJumpers } from './page-jumper'

jest.mock('../config', () => ({
    config: {
        buildType: 'assembler',
        apiBasePath: '/docs/_api',
        rootPath: '/docs',
    },
}))
jest.mock('../web-components/ModalSearch/pagefind', () => ({
    searchPagefind: jest.fn(),
}))

const markup = `
<form data-page-jumper>
  <input data-page-jumper-input role="combobox" aria-expanded="false">
  <div data-page-jumper-panel hidden>
    <div data-page-jumper-loading hidden></div>
    <p data-page-jumper-status role="status"></p>
    <ol id="pj-results" data-page-jumper-results role="listbox"></ol>
    <template data-search-result>
      <li role="option"><a href="#"><span data-search-path></span><span data-search-title></span><span data-search-excerpt></span></a></li>
    </template>
  </div>
</form>`

const flush = async () => {
    for (let i = 0; i < 5; i++) await Promise.resolve()
}

describe('page jumper', () => {
    beforeEach(() => {
        jest.useFakeTimers()
        document.body.innerHTML = markup
        global.fetch = jest.fn().mockResolvedValue({
            ok: true,
            json: async () => ({
                results: [
                    {
                        url: '/docs/a',
                        title: 'Alpha',
                        description: '',
                        parents: [{ title: 'Docs', url: '/docs' }],
                    },
                ],
            }),
        }) as unknown as typeof fetch
        initPageJumpers()
    })
    afterEach(() => jest.useRealTimers())

    const type = (value: string) => {
        const input = document.querySelector<HTMLInputElement>(
            '[data-page-jumper-input]'
        )!
        input.value = value
        input.dispatchEvent(new Event('input', { bubbles: true }))
        return input
    }

    it('lists matching pages under the field as you type', async () => {
        type('alp')
        jest.advanceTimersByTime(300)
        await flush()

        const panel = document.querySelector<HTMLElement>(
            '[data-page-jumper-panel]'
        )!
        expect(panel.hidden).toBe(false)
        expect(document.querySelectorAll('[role="option"]')).toHaveLength(1)
        expect(
            document.querySelector('[data-page-jumper-status]')!.textContent
        ).toBe('1 page.')
    })

    it('closes the panel when the field is cleared or Escape is pressed', async () => {
        const input = type('alp')
        jest.advanceTimersByTime(300)
        await flush()

        input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }))
        expect(
            document.querySelector<HTMLElement>('[data-page-jumper-panel]')!
                .hidden
        ).toBe(true)

        type('')
        expect(document.querySelectorAll('[role="option"]')).toHaveLength(0)
    })

    it('does nothing for a field the script has not found', () => {
        document.body.innerHTML = '<form><input name="q"></form>'
        expect(() => initPageJumpers()).not.toThrow()
    })
})
