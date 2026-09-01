import { journey, step, monitor, expect } from '@elastic/synthetics'

const VERSION_DROPDOWN = '[data-testid="docs-version-dropdown"]'

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
 * The docs version picker sits in the top bar and mobile overlay when
 * NAVIGATION_PREVIEW is on. It sits in the right rail and at the top of the
 * mobile page when the flag is off.
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

    step('Desktop version picker is visible and opens', async () => {
        const picker = page.locator(`${VERSION_DROPDOWN}:visible`)
        await expect(picker).toHaveCount(1)
        await expect(picker).toBeVisible()

        const button = picker.locator('button')
        await button.click()
        await expect(button).toHaveAttribute('aria-expanded', 'true')
        await expect(
            picker.getByText('Current', { exact: false })
        ).toBeVisible()
        await page.keyboard.press('Escape')
    })

    step('Mobile version picker is visible in the correct place', async () => {
        await page.setViewportSize({ width: 375, height: 800 })

        const navigationPreviewEnabled = await page
            .locator('body')
            .evaluate((body) => body.classList.contains('navigation-preview'))

        if (navigationPreviewEnabled) {
            await page
                .locator('label[for="pages-nav-hamburger"]')
                .first()
                .click()
        }

        const visiblePicker = page.locator(`${VERSION_DROPDOWN}:visible`)
        await expect(visiblePicker).toHaveCount(1)
        await expect(visiblePicker).toBeVisible()

        const expectedContainer = navigationPreviewEnabled
            ? page.locator('#pages-nav')
            : page.locator('#toc-nav')
        await expect(
            expectedContainer.locator(`${VERSION_DROPDOWN}:visible`)
        ).toHaveCount(1)
    })
})
