import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import path from 'node:path'
import { defineConfig } from 'vite'
import svgr from 'vite-plugin-svgr'

const docsBuilderVersion =
    process.env.DOCS_BUILDER_VERSION?.trim() ?? '0.0.0-dev'

const cssEntryNames: Record<string, string> = {
    styles: 'styles.css',
    assembler: 'assembler.css',
    isolated: 'isolated.css',
    codex: 'codex.css',
}

export default defineConfig({
    base: './',
    plugins: [
        react({ jsxImportSource: '@emotion/react' }),
        svgr(),
        tailwindcss(),
    ],
    define: {
        'process.env.DOCS_BUILDER_VERSION': JSON.stringify(docsBuilderVersion),
    },
    build: {
        outDir: '_static',
        emptyOutDir: false,
        sourcemap: true,
        // Razor resolves `main.js` to the hashed entry through `.vite/manifest.json`.
        manifest: true,
        // Tailwind's content globs match the built `_static/*.js`, and the plugin watches every
        // scanned file, so without this each `vite build --watch` output triggers another build.
        // Only set under --watch: any non-null value turns a plain `vite build` into a watcher.
        watch: process.argv.includes('--watch')
            ? { exclude: ['**/_static/**', '**/node_modules/**'] }
            : null,
        rollupOptions: {
            // htmx evaluates hx-on/hx-vals expressions with a direct eval; it is a vendored
            // dependency we can't change, so silence only that known warning.
            onwarn(warning, defaultHandler) {
                if (
                    warning.code === 'EVAL' &&
                    warning.id?.includes('node_modules/htmx.org/')
                ) {
                    return
                }
                defaultHandler(warning)
            },
            input: {
                main: path.resolve(__dirname, 'Assets/main.ts'),
                styles: path.resolve(__dirname, 'Assets/styles.css'),
                assembler: path.resolve(__dirname, 'Assets/assembler.css'),
                isolated: path.resolve(__dirname, 'Assets/isolated.css'),
                codex: path.resolve(__dirname, 'Assets/codex.css'),
            },
            output: {
                // Chunks import shared code back from the entry by file name. A content-hashed
                // name keeps that import and the <script> tag on one URL; `main.js?v=hash`
                // next to `./main.js` made the browser evaluate the entry twice.
                entryFileNames: (chunkInfo) => {
                    if (chunkInfo.name === 'main') {
                        return 'main-[hash].js'
                    }

                    return `${chunkInfo.name}.js`
                },
                chunkFileNames: 'chunk-[name]-[hash].js',
                assetFileNames: (assetInfo) => {
                    const originalName =
                        assetInfo.names?.[0] ?? assetInfo.name ?? ''
                    const cssName =
                        cssEntryNames[originalName.replace(/\.css$/, '')]
                    if (cssName) {
                        return cssName
                    }

                    if (originalName.endsWith('.css')) {
                        return 'chunk-[name]-[hash][extname]'
                    }

                    return 'asset-[name]-[hash][extname]'
                },
            },
        },
    },
})
