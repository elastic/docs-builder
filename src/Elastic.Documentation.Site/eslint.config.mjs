import js from '@eslint/js'
import { defineConfig, globalIgnores } from 'eslint/config'
import globals from 'globals'
import tseslint from 'typescript-eslint'

export default defineConfig([
	globalIgnores(['_static/main.js', 'tailwind.config.js']),
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
	{
		files: ['**/*.{js,jsx,mjs,cjs,ts,tsx}'],
		rules: {
			'no-console': [
				'error',
				{
					allow: ['warn', 'error'],
				},
			],
		},
	},
	{
		// Allow console.log in synthetics config (test configuration file)
		files: ['synthetics/**/*.ts'],
		rules: {
			'no-console': 'off',
		},
	},
])
