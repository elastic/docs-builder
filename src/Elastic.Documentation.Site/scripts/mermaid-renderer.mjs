// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

/**
 * CLI tool to render Mermaid diagrams to SVG using beautiful-mermaid.
 * Reads Mermaid code from stdin and outputs SVG to stdout.
 *
 * Usage: echo "graph LR; A --> B" | node mermaid-renderer.mjs
 */

import { renderMermaid } from 'beautiful-mermaid';

// High-contrast theme configuration
// beautiful-mermaid generates CSS variables that don't resolve correctly in all contexts,
// so we resolve them to actual colors during post-processing
const colors = {
	background: '#FFFFFF',
	foreground: '#000000',
	nodeFill: '#F5F5F5',
	nodeStroke: '#000000',
	line: '#000000',
	innerStroke: '#333333',
};

// Map CSS variables to resolved colors
const variableReplacements = {
	'--_text': colors.foreground,
	'--_text-sec': colors.foreground,
	'--_text-muted': colors.foreground,
	'--_text-faint': colors.foreground,  // "+ ", ": ", "(no attributes)"
	'--_line': colors.line,
	'--_arrow': colors.foreground,
	'--_node-fill': colors.nodeFill,
	'--_node-stroke': colors.nodeStroke,
	'--_inner-stroke': colors.innerStroke,
	'--bg': colors.background,
};

function resolveVariables(svg) {
	let result = svg;
	for (const [variable, color] of Object.entries(variableReplacements)) {
		const pattern = new RegExp(`(fill|stroke)="var\\(${variable.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}\\)"`, 'g');
		result = result.replace(pattern, `$1="${color}"`);
	}
	return result;
}

async function main() {
	const chunks = [];
	for await (const chunk of process.stdin) {
		chunks.push(chunk);
	}
	const mermaidCode = Buffer.concat(chunks).toString('utf-8').trim();

	if (!mermaidCode) {
		console.error('No Mermaid code provided');
		process.exit(1);
	}

	try {
		const svg = await renderMermaid(mermaidCode);
		process.stdout.write(resolveVariables(svg));
	} catch (error) {
		// Output just the error message - the C# caller adds context
		console.error(error.message);
		process.exit(1);
	}
}

main();
