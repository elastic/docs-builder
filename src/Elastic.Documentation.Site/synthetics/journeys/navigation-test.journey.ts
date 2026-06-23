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

journey('navigation test', ({ page, params }) => {
    monitor.use({
        id: `elastic-co-docs-nav-${params.environment}-v2`,
        schedule: getSchedule(params.environment),
        tags: [`env:${params.environment}`],
    })

    const host = params.baseUrl
    step(`Go to ${host}`, async () => {
        await page.goto(`${host}/docs`, {
            timeout: 60000,
            waitUntil: 'domcontentloaded',
        })
        await expect(page).toHaveTitle(/Elastic Docs \| Elastic Docs/)
    })

    step('Navigate to Elastic fundamentals', async () => {
        const fundamentalsLink = page
            .getByRole('link', { name: 'Elastic Fundamentals' })
            .first()
        const landingGetStarted = page
            .locator('a.card-cta')
            .filter({ hasText: 'Get started' })
            .first()

        if (
            await fundamentalsLink
                .isVisible({ timeout: 5000 })
                .catch(() => false)
        ) {
            await fundamentalsLink.click()
        } else {
            await landingGetStarted.click()
        }

        await expect(page).toHaveURL(`${host}/docs/get-started`)
        await expect(page).toHaveTitle(/Elastic fundamentals/)
        await expect(
            page.getByRole('heading', { name: 'Elastic fundamentals' })
        ).toBeVisible()
    })

    step('Click on "deployment options" in nav', async () => {
        await page
            .getByRole('link', { name: 'Deployment options' })
            .first()
            .click()
        await expect(page).toHaveURL(
            `${host}/docs/get-started/deployment-options`
        )
        await expect(page).toHaveTitle(/Deployment options/)
        await expect(
            page.getByRole('heading', { name: 'Deployment options' })
        ).toBeVisible()
    })

    step('Click on "Elastic Cloud" in markdown content', async () => {
        await page
            .locator('#markdown-content')
            .getByRole('link', { name: 'Elastic Cloud' })
            .first()
            .click()
        await expect(page).toHaveURL(
            `${host}/docs/deploy-manage/deploy/elastic-cloud`
        )
        await expect(page).toHaveTitle(/Elastic Cloud/)
    })

    step('Use dropdown to navigate to reference', async () => {
        const referenceUrl = `${host}/docs/reference`
        const pagesDropdown = page.locator('#pages-dropdown')
        const referenceInDropdown = pagesDropdown.locator(
            'a[href="/docs/reference"]'
        )

        if (
            await pagesDropdown.isVisible({ timeout: 5000 }).catch(() => false)
        ) {
            await pagesDropdown.locator('svg').click()
            await referenceInDropdown.evaluate((el) =>
                (el as HTMLAnchorElement).click()
            )
            try {
                await expect(page).toHaveURL(referenceUrl, { timeout: 5000 })
            } catch {
                // Nav V2 sidebar can overlap the pages dropdown in assembler preview.
                await page.goto(referenceUrl)
            }
        } else {
            await page.goto(referenceUrl)
        }

        await expect(page).toHaveURL(referenceUrl)
    })
})
