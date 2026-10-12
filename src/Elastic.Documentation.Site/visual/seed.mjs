// Seeds the visual baselines from a base ref, so a branch with every feature flag off can be compared against what that ref renders:
// npm run visual:seed [-- --ref origin/main] [isolated|assembler|codex ...]
// The baselines are the flag-off output of the ref. They are written to the ignored visual/__snapshots__/ and never committed.
import { cleanEnv } from './host.mjs'
import { git, withWorktree } from './worktree.mjs'
import { spawnSync } from 'node:child_process'
import { existsSync, readdirSync, rmSync, writeFileSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const here = dirname(fileURLToPath(import.meta.url))
const siteDir = resolve(here, '..')
const repoRoot = resolve(here, '../../..')
const siteRelative = 'src/Elastic.Documentation.Site'

const args = process.argv.slice(2)
const refIndex = args.indexOf('--ref')
const ref = refIndex === -1 ? 'origin/main' : args[refIndex + 1]
// Drop `--ref` and its value, and only those: without the option there is nothing to drop (an index of -1 would drop the first host).
const requested =
	refIndex === -1
		? args
		: args.filter((_, i) => i !== refIndex && i !== refIndex + 1)
const hosts =
	requested.length > 0
		? requested
		: [
				'isolated',
				'assembler',
				...(process.env.VISUAL_CODEX_CONFIG ? ['codex'] : []),
			]

function run(command, commandArgs, options = {}) {
	console.log(
		`${options.cwd ?? repoRoot}$ ${command} ${commandArgs.join(' ')}`
	)
	const result = spawnSync(command, commandArgs, {
		cwd: repoRoot,
		stdio: 'inherit',
		...options,
	})
	if (result.status !== 0) {
		throw new Error(
			`${command} ${commandArgs[0]} failed (${result.status})`
		)
	}
}

// Snapshots can contain content that is not public. Refuse to write them anywhere git would pick them up.
function assertSnapshotsIgnored() {
	const probe = join(siteRelative, 'visual/__snapshots__/probe.png')
	const ignored = spawnSync('git', ['check-ignore', '-q', probe], {
		cwd: repoRoot,
	})
	if (ignored.status !== 0) {
		throw new Error(
			`${probe} is not git-ignored; refusing to write baselines.`
		)
	}
}

assertSnapshotsIgnored()

// Baselines of the hosts being seeded are replaced whole, so files from an earlier seed never mix with the new ones.
function clearBaselines() {
	const root = join(here, '__snapshots__')
	if (!existsSync(root)) return
	for (const dir of readdirSync(root)) {
		if (hosts.some((host) => dir.startsWith(`${host}-`)))
			rmSync(join(root, dir), { recursive: true, force: true })
	}
}

clearBaselines()

run('git', ['fetch', '--quiet', 'origin'])
const sha = git(['rev-parse', `${ref}^{commit}`])

await withWorktree(sha, (worktree) => {
	const baseSite = join(worktree, siteRelative)
	run('npm', ['ci'], { cwd: baseSite })
	run('npm', ['run', 'build'], { cwd: baseSite })
	run('dotnet', ['build', 'src/tooling/docs-builder'], { cwd: worktree })

	// The runner and pages come from this branch; only the docs-builder binary and the isolated docs come from the ref.
	const env = {
		...cleanEnv(),
		DOCS_BUILDER_DLL: join(
			worktree,
			'.artifacts/bin/docs-builder/debug/docs-builder.dll'
		),
		// The binary reads its static files and config from the checkout it runs in, so run the whole site from the worktree.
		VISUAL_ROOT: worktree,
		VISUAL_DOCS_DIR: join(worktree, 'docs'),
		VISUAL_HOSTS: hosts.join(','),
	}
	const prepared = hosts.filter((h) => h !== 'isolated')
	if (prepared.length > 0) {
		run('node', ['visual/prepare.mjs', ...prepared], { cwd: siteDir, env })
	}
	run('npx', ['playwright', 'test', '-c', 'visual', '--update-snapshots'], {
		cwd: siteDir,
		env,
	})
})

writeFileSync(join(here, '__snapshots__', '.seed-ref'), `${sha}\n`)
console.log(
	`Seeded ${hosts.join(', ')} baselines from ${ref} (${sha}). Compare this branch with: npm run visual:check`
)
