import {mergeHTMLPlugin} from "./hljs-merge-html-plugin";
import hljs from "highlight.js";

hljs.registerLanguage('apiheader', function() {
	return {
		case_insensitive: true, // language is case-insensitive
		keywords: 'GET POST PUT DELETE HEAD OPTIONS PATCH',
		contains: [
			hljs.HASH_COMMENT_MODE,
			{
				className: "subst", // (pathname: path1/path2/dothis) color #ab5656
				begin: /(?<=(?:\/|GET |POST |PUT |DELETE |HEAD |OPTIONS |PATH))[^?\n\r\/]+/,
			}
		],		}
})

// https://tc39.es/ecma262/#sec-literals-numeric-literals
const decimalDigits = '[0-9](_?[0-9])*';
const frac = `\\.(${decimalDigits})`;
// DecimalIntegerLiteral, including Annex B NonOctalDecimalIntegerLiteral
// https://tc39.es/ecma262/#sec-additional-syntax-numeric-literals
const decimalInteger = `0|[1-9](_?[0-9])*|0[0-7]*[89][0-9]*`;
const NUMBER = {
	className: 'number',
	variants: [
		// DecimalLiteral
		{ begin: `(\\b(${decimalInteger})((${frac})|\\.)?|(${frac}))` +
				`[eE][+-]?(${decimalDigits})\\b` },
		{ begin: `\\b(${decimalInteger})\\b((${frac})\\b|\\.)?|(${frac})\\b` },

		// DecimalBigIntegerLiteral
		{ begin: `\\b(0|[1-9](_?[0-9])*)n\\b` },

		// NonDecimalIntegerLiteral
		{ begin: "\\b0[xX][0-9a-fA-F](_?[0-9a-fA-F])*n?\\b" },
		{ begin: "\\b0[bB][0-1](_?[0-1])*n?\\b" },
		{ begin: "\\b0[oO][0-7](_?[0-7])*n?\\b" },

		// LegacyOctalIntegerLiteral (does not include underscore separators)
		// https://tc39.es/ecma262/#sec-additional-syntax-numeric-literals
		{ begin: "\\b0[0-7]+n?\\b" },
	],
	relevance: 0
};


hljs.registerLanguage('eql', function() {
	return {
		case_insensitive: true, // language is case-insensitive
		keywords: {
			keyword: 'where sequence sample untill and or not in in~',
			literal: ['false','true','null'],
			'subst': 'add between cidrMatch concat divide endsWith indexOf length modulo multiply number startsWith string stringContains substring subtract'
		},
		contains: [
			hljs.QUOTE_STRING_MODE,
			hljs.C_LINE_COMMENT_MODE,
			{
				scope: "operator", // (pathname: path1/path2/dothis) color #ab5656
				match: /(?:<|<=|==|:|!=|>=|>|like~?|regex~?)/,
			},
			{
				scope: "punctuation", // (pathname: path1/path2/dothis) color #ab5656
				match: /(?:!?\[|\]|\|)/,
			},
			NUMBER,

		]
	}
})

hljs.addPlugin(mergeHTMLPlugin);
export function initHighlight() {

	hljs.highlightAll();
}
