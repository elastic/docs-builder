import { initApiDocs } from './api-docs'

function endpointMarkup(): string {
    return `
        <header class="api-page-intro">
            <ul id="paths" class="api-url-listing">
                <li class="api-url-list-item"><span class="api-url-row">
                    <span class="api-url"><span class="api-url-path">/{index}/_create/{id}</span></span>
                    <button type="button" class="copybtn api-url-copy" data-copy="/{index}/_create/{id}"></button>
                </span></li>
                <li class="api-url-list-item"><span class="api-url-row">
                    <span class="api-url"><span class="api-url-path">/_create</span></span>
                    <button type="button" class="copybtn api-url-copy" data-copy="/_create"></button>
                </span></li>
            </ul>
        </header>
        <section id="elastic-api-v3"></section>
    `
}

describe('endpoint path copy', () => {
    const writeText = jest.fn().mockResolvedValue(undefined)

    beforeAll(() => {
        Object.defineProperty(navigator, 'clipboard', {
            value: { writeText },
            configurable: true,
        })
    })

    beforeEach(() => {
        writeText.mockClear()
        document.body.innerHTML = endpointMarkup()
        initApiDocs()
    })

    it('copies the route of the row whose button is clicked', () => {
        document
            .querySelectorAll<HTMLButtonElement>('button.api-url-copy')[1]
            .click()

        expect(writeText).toHaveBeenCalledWith('/_create')
    })
})
