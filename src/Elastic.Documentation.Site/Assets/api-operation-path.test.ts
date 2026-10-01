import { applyOperationPath } from './api-docs'

function markup(): string {
    return `
        <ul id="paths">
            <li><label class="api-url-row current">
                <input type="radio" data-path-key="get /_cluster/health" checked />
            </label></li>
            <li><label class="api-url-row">
                <input type="radio" data-path-key="get /_cluster/health/{index}" />
            </label></li>
        </ul>
        <span class="required type-status" data-required-for="/_cluster/health/{index}" hidden>required</span>
        <div data-example-key="get /_cluster/health">GET _cluster/health</div>
        <div data-example-key="get /_cluster/health/{index}" hidden>GET /_cluster/health/{index}</div>
    `
}

describe('applyOperationPath', () => {
    it('marks index required only on the index path and shows that example', () => {
        document.body.innerHTML = markup()

        applyOperationPath(document, 'get /_cluster/health/{index}')

        const required = document.querySelector<HTMLElement>(
            '[data-required-for]'
        )
        expect(required?.hasAttribute('hidden')).toBe(false)
        const visible = [
            ...document.querySelectorAll<HTMLElement>('[data-example-key]'),
        ].filter((panel) => !panel.hasAttribute('hidden'))
        expect(visible).toHaveLength(1)
        expect(visible[0].dataset.exampleKey).toBe(
            'get /_cluster/health/{index}'
        )
        expect(visible[0].textContent).toContain('GET /_cluster/health/{index}')

        applyOperationPath(document, 'get /_cluster/health')

        expect(required?.hasAttribute('hidden')).toBe(true)
        const bare = [
            ...document.querySelectorAll<HTMLElement>('[data-example-key]'),
        ].find((panel) => !panel.hasAttribute('hidden'))
        expect(bare?.textContent).toContain('GET _cluster/health')
    })
})
