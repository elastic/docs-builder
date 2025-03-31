import { defineConfig, globalIgnores } from 'eslint/config'
import globals from 'globals'
import js from '@eslint/js'
import tseslint from 'typescript-eslint'

export default defineConfig([
	globalIgnores(['_static/main.js']),
	{ files: ['**/*.{js,mjs,cjs,ts}'] },
	{
		files: ['**/*.{js,mjs,cjs,ts}'],
		languageOptions: { globals: globals.browser },
	},
	{
		files: ['**/*.{js,mjs,cjs,ts}'],
		plugins: { js },
		extends: ['js/recommended'],
	},
	tseslint.configs.recommended,
])
