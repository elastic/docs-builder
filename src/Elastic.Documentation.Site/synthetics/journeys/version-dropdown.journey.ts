import { journey, step, monitor, expect } from '@elastic/synthetics'

function getSchedule(env: string) {
    const scheduleMapping = {
        local: 15,
        edge: 15,
        staging: 15,
        prod: 1,
    }
    return scheduleMapping[env] || 15
}

/**
 * The docs version picker sits in the top bar when NAVIGATION_PREVIEW is on
 * and in the right rail when the flag is off. Both hosts share
 * #docs-version-dropdown so this journey covers either placement.
 */
journey('version dropdown', ({ page, params }) => {
    monitor.use({
        id: `elastic-co-docs-version-dropdown-${params.environment}-v1`,
        schedule: getSchedule(params.environment),
        tags: [`env:${params.environment}`],
    })

    const docsRoot = params.docsRoot as string

    step('Open a versioned docs page', async () => {
        await page.setViewportSize({ width: 1280, height: 800 })
        await page.goto(`${docsRoot}/get-started`, {
            timeout: 60000,
            waitUntil: 'domcontentloaded',
        })
        await expect(
            page.getByRole('heading', { name: 'Elastic fundamentals' })
        ).toBeVisible()
    })

    step('Version picker is visible and opens', async () => {
        const picker = page.locator('#docs-version-dropdown')
        await expect(picker).toBeVisible()

        const button = picker.locator('button')
        await button.click()
        await expect(button).toHaveAttribute('aria-expanded', 'true')
        await expect(
            picker.getByText('Current', { exact: false })
        ).toBeVisible()
    })
})
