/**
 * Maps the hostname the docs are served from to the `deployment.environment` resource attribute.
 *
 * The JavaScript is pre-built and bundled into the docs-builder CLI, so the environment can only come
 * from the hostname at runtime. The labels match the ones the backends report, so the frontend and the
 * API can be filtered together: the docs API sets edge, staging, prod and demo, and the Codex API sets
 * internal.
 */
const ENVIRONMENT_BY_HOSTNAME: Record<string, string> = {
    'www.elastic.co': 'prod',
    'elastic.co': 'prod',
    'staging-website.elastic.co': 'staging',
    'd34ipnu52o64md.cloudfront.net': 'edge',
    'docs-edge.elastic.dev': 'edge',
    'd3fvim9nkr2bzu.cloudfront.net': 'demo',
    'docs-demo.elastic.dev': 'demo',
    'codex.elastic.dev': 'internal',
    localhost: 'local',
    '127.0.0.1': 'local',
}

/** Hostnames are case-insensitive, and a fully qualified name ends with a dot (`www.elastic.co.`). */
function normalizeHostname(hostname: string): string {
    return hostname.toLowerCase().replace(/\.$/, '')
}

export function detectEnvironment(
    hostname: string = window.location.hostname
): string {
    return ENVIRONMENT_BY_HOSTNAME[normalizeHostname(hostname)] ?? 'unknown'
}
