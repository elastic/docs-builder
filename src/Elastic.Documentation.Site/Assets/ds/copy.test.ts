import { initCopyButtons } from './copy'

const block = `
<figure class="code-block">
    <figcaption><span>JSON</span><button hidden data-copy type="button">Copy</button></figcaption>
    <pre><code>line one<span class="code-callout" data-index="1"></span>
line two
</code></pre>
</figure>`

describe('initCopyButtons', () => {
    let written: string[]

    beforeEach(() => {
        written = []
        Object.assign(navigator, {
            clipboard: {
                writeText: jest.fn(
                    async (text: string) => void written.push(text)
                ),
            },
        })
        document.body.innerHTML = block
    })

    it('reveals the button and copies the code without callouts', async () => {
        initCopyButtons()
        const button = document.querySelector<HTMLButtonElement>('[data-copy]')!

        expect(button.hidden).toBe(false)
        button.click()
        await Promise.resolve()

        expect(written).toEqual(['line one\nline two'])
    })

    it('marks the button as copied', async () => {
        initCopyButtons()
        const button = document.querySelector<HTMLButtonElement>('[data-copy]')!

        button.click()
        await new Promise((resolve) => setTimeout(resolve, 0))

        expect(button.dataset.copied).toBeDefined()
    })

    it('leaves the button hidden when the clipboard is unavailable', () => {
        Object.assign(navigator, { clipboard: undefined })
        initCopyButtons()

        expect(
            document.querySelector<HTMLButtonElement>('[data-copy]')!.hidden
        ).toBe(true)
    })

    it('binds once', () => {
        initCopyButtons()
        initCopyButtons()
        document.querySelector<HTMLButtonElement>('[data-copy]')!.click()

        expect(navigator.clipboard.writeText).toHaveBeenCalledTimes(1)
    })
})
