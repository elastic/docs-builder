import { journey, step, monitor, expect } from '@elastic/synthetics'

journey('404 recovery page', ({ page, params }) => {
    monitor.use({
        id: `elastic-co-docs-404-${params.environment}`,
        schedule: params.environment === 'prod' ? 1 : 15,
        tags: [`env:${params.environment}`],
    })

    const host = params.baseUrl
    step('Open a missing documentation page', async () => {
        const response = await page.goto(
            `${host}/docs/this-page-does-not-exist-for-404-test`,
            { waitUntil: 'domcontentloaded' }
        )

        expect(response?.status()).toBe(404)
        await expect(
            page.getByRole('heading', { name: 'Page not found' })
        ).toBeVisible()
    })
})
