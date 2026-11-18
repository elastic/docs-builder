import { detectEnvironment } from './environment'

describe('detectEnvironment', () => {
    it.each([
        ['www.elastic.co', 'prod'],
        ['elastic.co', 'prod'],
        ['staging-website.elastic.co', 'staging'],
        ['d34ipnu52o64md.cloudfront.net', 'edge'],
        ['docs-edge.elastic.dev', 'edge'],
        ['d3fvim9nkr2bzu.cloudfront.net', 'demo'],
        ['docs-demo.elastic.dev', 'demo'],
        ['codex.elastic.dev', 'internal'],
        ['localhost', 'local'],
        ['127.0.0.1', 'local'],
    ])('maps %s to %s', (hostname, environment) => {
        expect(detectEnvironment(hostname)).toBe(environment)
    })

    it.each([
        ['www.elastic.co.', 'prod'],
        ['WWW.Elastic.CO', 'prod'],
        ['Codex.Elastic.Dev.', 'internal'],
    ])('ignores case and a trailing dot: %s is %s', (hostname, environment) => {
        expect(detectEnvironment(hostname)).toBe(environment)
    })

    it.each([
        'qa-www.elastic.co',
        'elastic.cloud-croc.ru',
        'siteproxy.ruqli.workers.dev',
        'example.com',
        '',
    ])(
        'leaves other hosts, such as mirrors and proxies, as unknown: "%s"',
        (hostname) => {
            expect(detectEnvironment(hostname)).toBe('unknown')
        }
    )

    it('uses the hostname of the current page by default', () => {
        // jsdom serves tests from localhost
        expect(detectEnvironment()).toBe('local')
    })
})
