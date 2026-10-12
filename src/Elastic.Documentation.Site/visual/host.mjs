// Starts one docs-builder host for the visual tests: node visual/host.mjs <isolated|assembler|codex> <port>
// Feature flags are scrubbed from the child's environment so a flag exported in the developer's shell can never leak into a baseline.
import { spawn } from 'node:child_process'
import { existsSync, readFileSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const here = dirname(fileURLToPath(import.meta.url))
// VISUAL_ROOT points at another checkout whose binary, static files and config are used instead of this one, as the seed does for its base ref.
const repoRoot = process.env.VISUAL_ROOT ?? resolve(here, '../../..')
const dll =
	process.env.DOCS_BUILDER_DLL ??
	join(repoRoot, '.artifacts/bin/docs-builder/debug/docs-builder.dll')

const [mode, port] = process.argv.slice(2)

export function cleanEnv(extraFlags = {}) {
	const env = { ...process.env }
	for (const key of Object.keys(env)) {
		if (key.toUpperCase().startsWith('FEATURE_')) delete env[key]
	}
	for (const [name, value] of Object.entries(extraFlags)) {
		env[`FEATURE_${name}`] = value
	}
	return env
}

export function assertNavigationPreviewOffForDev() {
	const yaml = readFileSync(
		join(repoRoot, 'config/assembler.yml'),
		'utf8'
	).replace(/\r\n/g, '\n')
	const dev = yaml.match(/^ {2}dev:\n((?: {4}.*\n|\n)*)/m)
	const flag = dev?.[1].match(/^ {6}NAVIGATION_PREVIEW:\s*(\S+)/m)?.[1]
	if (flag && flag.toLowerCase() !== 'false') {
		throw new Error(
			"NAVIGATION_PREVIEW is enabled for 'dev' in config/assembler.yml; the flag-off baseline would be invalid."
		)
	}
}

function commandFor(hostMode, hostPort) {
	switch (hostMode) {
		case 'isolated':
			return {
				cwd: process.env.VISUAL_DOCS_DIR ?? join(repoRoot, 'docs'),
				args: ['serve', '--port', hostPort, '--no-hud'],
			}
		case 'assembler':
			assertNavigationPreviewOffForDev()
			return {
				cwd: repoRoot,
				args: [
					'assembler',
					'serve',
					'--port',
					hostPort,
					'--path',
					join(repoRoot, '.artifacts/assembly'),
				],
			}
		case 'codex':
			return {
				cwd: repoRoot,
				args: [
					'codex',
					'serve',
					'--port',
					hostPort,
					'--path',
					join(repoRoot, '.artifacts/codex/docs'),
				],
			}
		default:
			throw new Error(`Unknown host '${hostMode}'`)
	}
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
	if (!existsSync(dll)) {
		throw new Error(
			`${dll} not found. Run 'dotnet build src/tooling/docs-builder' first.`
		)
	}
	const { cwd, args } = commandFor(mode, port)
	const child = spawn('dotnet', [dll, ...args], {
		cwd,
		env: cleanEnv(),
		stdio: 'inherit',
	})
	for (const signal of ['SIGINT', 'SIGTERM']) {
		process.on(signal, () => child.kill(signal))
	}
	child.on('exit', (code) => process.exit(code ?? 0))
}
