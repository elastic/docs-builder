// Compares this branch, with every feature flag off, against the baselines that `npm run visual:seed` saved: npm run visual:check
// The isolated site serves docs/ from the seeded commit, so a docs change in this branch is not mistaken for a rendering change.
import { cleanEnv } from './host.mjs'
import { git, repoRoot, withWorktree } from './worktree.mjs'
import { spawnSync } from 'node:child_process'
import { existsSync, readFileSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const here = dirname(fileURLToPath(import.meta.url))
const siteDir = resolve(here, '..')
const seedRef = join(here, '__snapshots__', '.seed-ref')

if (!existsSync(seedRef)) {
	throw new Error('No baselines found. Run `npm run visual:seed` first.')
}
const sha = readFileSync(seedRef, 'utf8').trim()
git(['cat-file', '-e', `${sha}^{commit}`])

function run(command, args, env, cwd = siteDir) {
	console.log(`${command} ${args.join(' ')}`)
	const result = spawnSync(command, args, {
		cwd,
		env,
		stdio: 'inherit',
	})
	if (result.status !== 0) {
		throw new Error(`${command} ${args[0]} failed (${result.status})`)
	}
}

await withWorktree(sha, (worktree) => {
	// The comparison must exercise this checkout, so build it first; the debug binary on disk may predate the last edit.
	run('dotnet', ['build', 'src/tooling/docs-builder'], process.env, repoRoot)
	const env = { ...cleanEnv(), VISUAL_DOCS_DIR: join(worktree, 'docs') }
	run('node', ['visual/prepare.mjs'], env)
	run('npx', ['playwright', 'test', '-c', 'visual'], env)
})
console.log(`Compared ${repoRoot} with the baselines seeded from ${sha}.`)
