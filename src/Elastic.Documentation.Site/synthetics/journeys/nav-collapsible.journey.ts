import { journey, step, monitor, expect } from '@elastic/synthetics'

const SCENARIO = {
    startPath: '/deploy-manage',
    folderHrefSuffix: '/deploy-manage/deploy',
    nestedHrefSuffix: '/deploy-manage/deploy/elastic-cloud',
    nestedName: 'Elastic Cloud',
}

function getSchedule(env: string) {
    const scheduleMapping = {
        local: 15,
        edge: 15,
        staging: 15,
        prod: 1,
    }
    return scheduleMapping[env] || 15
}

function sidebarHref(scope: string, suffix: string) {
    return `${scope}[href$="${suffix}"], ${scope}[href$="${suffix}/"]`
}

journey('nav collapsible', ({ page, params }) => {
    monitor.use({
        id: `elastic-co-docs-nav-collapsible-${params.environment}-v1`,
        schedule: getSchedule(params.environment),
        tags: [`env:${params.environment}`],
    })

    const docsRoot = params.docsRoot as string
    const pagesNav = page.locator('#pages-nav')
    const deployFolder = pagesNav.locator('li.nav-folder').filter({
        has: page.locator(
            sidebarHref(
                ':scope > .peer > a.sidebar-link',
                SCENARIO.folderHrefSuffix
            )
        ),
    })
    const nestedLink = pagesNav.locator(
        sidebarHref('a.sidebar-link', SCENARIO.nestedHrefSuffix)
    )

    step('Open a section with collapsed nav folders', async () => {
        await page.setViewportSize({ width: 1280, height: 800 })
        await page.goto(`${docsRoot}${SCENARIO.startPath}`, {
            timeout: 60000,
            waitUntil: 'domcontentloaded',
        })
        await expect(
            page.getByRole('heading', {
                name: 'Deploy and manage',
                exact: true,
            })
        ).toBeVisible()
    })

    step('Open the Deploy folder and reveal a nested link', async () => {
        await expect(deployFolder).toBeVisible()
        await expect(nestedLink).not.toBeVisible()

        await deployFolder
            .locator('.nav-folder-chevron, label, .nav-toggle-btn')
            .first()
            .click()

        await expect(nestedLink).toBeVisible()
    })

    step('Click the nested Elastic Cloud sidebar link', async () => {
        await nestedLink.click()
        await expect(page).toHaveURL(
            new RegExp(`${SCENARIO.nestedHrefSuffix}/?$`)
        )
        await expect(
            page.getByRole('heading', {
                name: SCENARIO.nestedName,
                exact: true,
            })
        ).toBeVisible()
    })
})
