import { initFeedback } from './feedback'

const widget = `
<section data-feedback hidden data-feedback-page-url="/docs/page" data-feedback-page-title="Page" data-feedback-api="/docs/_api/v1/page-feedback" data-feedback-reason-version="2">
  <div data-feedback-prompt>
    <button type="button" data-feedback-reaction="thumbsUp" aria-pressed="false">Yes</button>
    <button type="button" data-feedback-reaction="thumbsDown" aria-pressed="false">No</button>
  </div>
  <form data-feedback-form hidden>
    <fieldset data-feedback-reasons="thumbsUp" hidden>
      <input type="checkbox" value="accurate" data-feedback-reason>
      <input type="checkbox" value="easyToFind" data-feedback-reason>
    </fieldset>
    <fieldset data-feedback-reasons="thumbsDown" hidden>
      <input type="checkbox" value="outOfDate" data-feedback-reason>
    </fieldset>
    <textarea data-feedback-comment></textarea>
    <span data-feedback-count></span>
    <button type="submit" data-feedback-submit disabled>Submit</button>
    <p data-feedback-error hidden>error</p>
  </form>
  <p data-feedback-thanks hidden>Thanks</p>
</section>`

const $ = <T extends Element>(selector: string) =>
    document.querySelector<T>(selector)!
const click = (selector: string) => $<HTMLElement>(selector).click()
const check = (value: string) => {
    const box = $<HTMLInputElement>(`[value="${value}"]`)
    box.checked = true
    box.dispatchEvent(new Event('change', { bubbles: true }))
}
const submit = () =>
    $<HTMLFormElement>('[data-feedback-form]').dispatchEvent(
        new Event('submit', { cancelable: true, bubbles: true })
    )

describe('initFeedback', () => {
    let fetchMock: jest.Mock

    beforeEach(() => {
        jest.useFakeTimers()
        sessionStorage.clear()
        fetchMock = jest.fn(async () => ({ ok: true, status: 200 }))
        global.fetch = fetchMock as unknown as typeof fetch
        Object.defineProperty(globalThis, 'crypto', {
            value: { randomUUID: () => 'feedback-1' },
            configurable: true,
        })
        document.body.innerHTML = widget
        initFeedback()
    })

    afterEach(() => jest.useRealTimers())

    it('reveals the widget once the script runs', () => {
        expect($<HTMLElement>('[data-feedback]').hidden).toBe(false)
    })

    it('shows only the reasons that match the reaction', () => {
        click('[data-feedback-reaction="thumbsUp"]')

        expect($<HTMLElement>('[data-feedback-form]').hidden).toBe(false)
        expect(
            $<HTMLElement>('[data-feedback-reasons="thumbsUp"]').hidden
        ).toBe(false)
        expect(
            $<HTMLElement>('[data-feedback-reasons="thumbsDown"]').hidden
        ).toBe(true)
        expect(
            $('[data-feedback-reaction="thumbsUp"]').getAttribute(
                'aria-pressed'
            )
        ).toBe('true')
    })

    it('clears reasons that belong to the other reaction', () => {
        click('[data-feedback-reaction="thumbsUp"]')
        check('accurate')
        click('[data-feedback-reaction="thumbsDown"]')

        expect($<HTMLInputElement>('[value="accurate"]').checked).toBe(false)
    })

    it('enables submit only once a reason is chosen', () => {
        click('[data-feedback-reaction="thumbsUp"]')
        expect($<HTMLButtonElement>('[data-feedback-submit]').disabled).toBe(
            true
        )

        check('accurate')

        expect($<HTMLButtonElement>('[data-feedback-submit]').disabled).toBe(
            false
        )
    })

    it('saves the reaction alone after a short pause, and only the last one', async () => {
        click('[data-feedback-reaction="thumbsUp"]')
        click('[data-feedback-reaction="thumbsDown"]')
        await jest.advanceTimersByTimeAsync(450)

        expect(fetchMock).toHaveBeenCalledTimes(1)
        const [url, init] = fetchMock.mock.calls[0]
        expect(url).toBe('/docs/_api/v1/page-feedback/feedback-1')
        expect(JSON.parse(init.body)).toEqual({
            pageUrl: '/docs/page',
            pageTitle: 'Page',
            reaction: 'thumbsDown',
        })
    })

    it('sends the reasons, the version and the trimmed comment, then says thanks', async () => {
        click('[data-feedback-reaction="thumbsUp"]')
        check('accurate')
        $<HTMLTextAreaElement>('[data-feedback-comment]').value = '  great  '
        $<HTMLTextAreaElement>('[data-feedback-comment]').dispatchEvent(
            new Event('input', { bubbles: true })
        )
        submit()
        await jest.advanceTimersByTimeAsync(450)

        expect(
            JSON.parse(
                fetchMock.mock.calls[fetchMock.mock.calls.length - 1][1].body
            )
        ).toEqual({
            pageUrl: '/docs/page',
            pageTitle: 'Page',
            reaction: 'thumbsUp',
            reasons: ['accurate'],
            reasonSetVersion: 2,
            comment: 'great',
        })
        expect($<HTMLElement>('[data-feedback-thanks]').hidden).toBe(false)
        expect($<HTMLElement>('[data-feedback-form]').hidden).toBe(true)
        expect(
            sessionStorage.getItem('docs-page-feedback:/docs/page')
        ).toBeNull()
    })

    it('shows an error and offers a retry when saving fails', async () => {
        click('[data-feedback-reaction="thumbsUp"]')
        check('accurate')
        fetchMock.mockImplementation(async () => ({ ok: false, status: 500 }))
        submit()
        await jest.advanceTimersByTimeAsync(450)

        expect($<HTMLElement>('[data-feedback-error]').hidden).toBe(false)
        expect($<HTMLButtonElement>('[data-feedback-submit]').textContent).toBe(
            'Try again'
        )
        expect($<HTMLElement>('[data-feedback-thanks]').hidden).toBe(true)
    })

    it('counts the comment length', () => {
        click('[data-feedback-reaction="thumbsUp"]')
        $<HTMLTextAreaElement>('[data-feedback-comment]').value = 'hello'
        $<HTMLTextAreaElement>('[data-feedback-comment]').dispatchEvent(
            new Event('input', { bubbles: true })
        )

        expect($('[data-feedback-count]').textContent).toBe('5/2000')
    })

    it('restores an unfinished draft', () => {
        sessionStorage.setItem(
            'docs-page-feedback:/docs/page',
            JSON.stringify({
                feedbackId: 'saved-id',
                reaction: 'thumbsDown',
                reasons: ['outOfDate', 'bogus'],
                comment: 'old',
            })
        )
        document.body.innerHTML = widget
        initFeedback()

        expect(
            $('[data-feedback-reaction="thumbsDown"]').getAttribute(
                'aria-pressed'
            )
        ).toBe('true')
        expect($<HTMLInputElement>('[value="outOfDate"]').checked).toBe(true)
        expect($<HTMLTextAreaElement>('[data-feedback-comment]').value).toBe(
            'old'
        )
    })

    it('binds each widget once', () => {
        initFeedback()
        click('[data-feedback-reaction="thumbsUp"]')

        expect(
            $('[data-feedback-reaction="thumbsUp"]').getAttribute(
                'aria-pressed'
            )
        ).toBe('true')
    })
})
