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

    step('Click on "Elastic Fundamentals"', async () => {
        await page
            .getByRole('link', { name: 'Elastic Fundamentals' })
            .first()
            .click()
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
        const pagesDropdown = page.locator('#pages-dropdown')
        const svg = pagesDropdown.locator('svg')
        await svg.click()
        await pagesDropdown
            .getByRole('link', { name: 'Reference', exact: true })
            .click()
        await expect(page).toHaveURL(`${host}/docs/reference`)
    })
})
