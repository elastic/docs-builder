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
        await expect(
            page
                .locator('#main-container')
                .getByRole('textbox', { name: 'Search Elastic Docs' })
        ).toBeVisible()
        await expect(
            page.getByRole('link', { name: 'Go to docs home' })
        ).toBeVisible()
        await expect(
            page.getByRole('link', { name: 'Open full search' })
        ).toBeVisible()
    })
})
