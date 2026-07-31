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
 * Walks the main navigation paths and, along the way, verifies the htmx
 * navigation model: boosted links do a whole-body swap with hx-preserve
 * islands. A `window` marker set on first load survives an htmx swap but
 * not a full page load, so it distinguishes SPA navigation from a reload.
 */
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
        await page.evaluate(() => {
            window['__synthNoReload'] = true
        })
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
        // Tag the nav tree so later steps can tell preserved DOM from a swap
        await page.evaluate(() => {
            const navTree = document.querySelector('[id^="nav-tree"]')
            if (navTree) navTree['__synthOriginal'] = true
        })
    })

    step('Use accessible page navigation and image dialog', async () => {
        const skipLink = page.getByRole('link', {
            name: 'Skip to main content',
        })
        await skipLink.focus()
        await skipLink.press('Enter')
        await expect(page.locator('#main-container')).toBeFocused()

        await expect(
            page.getByRole('navigation', { name: 'Documentation sections' })
        ).toBeAttached()
        await expect(
            page.getByRole('navigation', {
                name: 'Page tools and contents',
            })
        ).toBeAttached()

        const imageTrigger = page.getByRole('button', {
            name: 'Image preview: The Elastic platform',
        })
        await imageTrigger.click()
        const dialog = page.getByRole('dialog', {
            name: 'Image preview: The Elastic platform',
        })
        await expect(dialog).toBeVisible()
        await expect(
            dialog.getByRole('button', { name: 'Close image preview' })
        ).toBeFocused()
        await page.keyboard.press('Escape')
        await expect(dialog).toBeHidden()
        await expect(imageTrigger).toBeFocused()
    })

    step('Click on "deployment options" in nav', async () => {
        // Expand a collapsed nav section so we can assert its state survives
        const expandedId = await page.evaluate(() => {
            const button = document.querySelector<HTMLButtonElement>(
                '[id^="nav-tree"] button[data-nav-toggle][aria-expanded="false"]'
            )
            button?.click()
            return button?.getAttribute('aria-controls') ?? null
        })

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

        // Same-group navigation: no reload, nav tree DOM (and state) preserved
        const state = await page.evaluate((id) => {
            const navTree = document.querySelector('[id^="nav-tree"]')
            return {
                noReload: window['__synthNoReload'] === true,
                navTreePreserved: navTree?.['__synthOriginal'] === true,
                sectionStillExpanded: id
                    ? document
                          .querySelector(
                              `button[data-nav-toggle][aria-controls="${CSS.escape(id)}"]`
                          )
                          ?.getAttribute('aria-expanded') === 'true'
                    : null,
            }
        }, expandedId)
        expect(state.noReload).toBe(true)
        expect(state.navTreePreserved).toBe(true)
        if (expandedId) expect(state.sectionStillExpanded).toBe(true)
    })

    step('Click on "Elastic Cloud" in markdown content', async () => {
        const treeIdBefore = await page.evaluate(
            () => document.querySelector('[id^="nav-tree"]')?.id
        )
        await page
            .locator('#markdown-content')
            .getByRole('link', { name: 'Elastic Cloud' })
            .first()
            .click()
        await expect(page).toHaveURL(
            `${host}/docs/deploy-manage/deploy/elastic-cloud`
        )
        await expect(page).toHaveTitle(/Elastic Cloud/)

        // Cross-group navigation: still no reload, but the nav tree is replaced
        const state = await page.evaluate(() => {
            const navTree = document.querySelector('[id^="nav-tree"]')
            return {
                noReload: window['__synthNoReload'] === true,
                treeId: navTree?.id,
                treeIsNewNode: navTree?.['__synthOriginal'] === undefined,
                treeShowsNewGroup:
                    navTree?.querySelector('a[href*="/deploy-manage/"]') !==
                    null,
            }
        })
        expect(state.noReload).toBe(true)
        expect(state.treeId).not.toBe(treeIdBefore)
        expect(state.treeIsNewNode).toBe(true)
        expect(state.treeShowsNewGroup).toBe(true)
    })

    step('Use dropdown to navigate to reference', async () => {
        const pagesDropdown = page.locator('#pages-dropdown')
        const dropdownButton = pagesDropdown.getByRole('button', {
            name: 'Choose a documentation section',
        })
        await dropdownButton.click()
        await expect(page.locator('#pages-dropdown-menu')).toBeVisible()
        await page.locator('#markdown-content').click()
        await expect(page.locator('#pages-dropdown-menu')).toBeHidden()

        await dropdownButton.click()
        await pagesDropdown
            .getByRole('link', { name: 'Reference', exact: true })
            .click()
        await expect(page).toHaveURL(`${host}/docs/reference`)
    })

    step(
        'Global nav script executed only once across navigations',
        async () => {
            const navJsFetches = await page.evaluate(
                () =>
                    performance
                        .getEntriesByType('resource')
                        .filter((e) => e.name.includes('elastic-nav.js')).length
            )
            // elastic-nav.js lives in <head> (head-support keeps it) and must not
            // re-execute per navigation; 0 covers environments without the global nav
            expect(navJsFetches).toBeLessThanOrEqual(1)
        }
    )

    step('/docs/api link triggers a full page load, not htmx', async () => {
        await page.evaluate(() => {
            const a = document.createElement('a')
            a.href = '/docs/api/'
            a.id = 'synthetic-api-link'
            a.textContent = 'api'
            document.querySelector('#content-container')?.appendChild(a)
        })
        // A full page load is a navigation request; an htmx request would be an
        // XHR carrying the ?v= cache-buster. Status doesn't matter (404 locally).
        const [request] = await Promise.all([
            page.waitForRequest(
                (req) => req.url().startsWith(`${host}/docs/api/`),
                { timeout: 30000 }
            ),
            page.locator('#synthetic-api-link').click(),
        ])
        expect(request.isNavigationRequest()).toBe(true)
        expect(new URL(request.url()).searchParams.has('v')).toBe(false)
    })
})
