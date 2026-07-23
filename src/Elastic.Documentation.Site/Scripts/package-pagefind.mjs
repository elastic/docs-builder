import { mkdirSync, readFileSync, writeFileSync } from 'node:fs'
import { dirname, join } from 'node:path'
import process from 'node:process'
import { fileURLToPath } from 'node:url'
import { gzipSync } from 'node:zlib'

const platformNames = {
	darwin: 'darwin',
	linux: 'linux',
	win32: 'windows',
}

const platform = platformNames[process.platform]
if (!platform || !['arm64', 'x64'].includes(process.arch)) {
	throw new Error(
		`Pagefind is not packaged for ${process.platform}-${process.arch}`
	)
}

const root = dirname(dirname(fileURLToPath(import.meta.url)))
const executable =
	process.platform === 'win32' ? 'pagefind_extended.exe' : 'pagefind_extended'
const source = join(
	root,
	'node_modules',
	'@pagefind',
	`${platform}-${process.arch}`,
	'bin',
	executable
)
const destination = join(root, 'obj', 'pagefind-indexer.gz')

mkdirSync(dirname(destination), { recursive: true })
writeFileSync(destination, gzipSync(readFileSync(source), { level: 9 }))
