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

hljs.registerLanguage('esql', function() {
	return {
		case_insensitive: true, // language is case-insensitive
		keywords: {
			keyword: 'FROM ROW SHOW DISSECT DROP ENRICH EVAL GROK KEEP LIMIT RENAME SORT STATS WHERE METADATA',
			literal: ['false','true','null'],
			function: [
				// aggregate
				"AVG", "COUNT", "COUNT_DISTINCT", "MAX", "MEDIAN", "MEDIAN_ABSOLUTE_DEVIATION", "MIN",
				"PERCENTILE", "SUM", "TOP", "VALUES", "WEIGHTED_AVG", "BUCKET",

				// conditional
				"CASE", "COALESCE", "GREATEST", "LEAST",

				//Date
				"DATE_DIFF", "DATE_EXTRACT", "DATE_FORMAT", "DATE_PARSE", "DATE_TRUNC", "NOW",

				//ip
				"CIDR_MATCH", "IP_PREFIX",

				//math
				"ABS", "ACOS", "ASIN", "ATAN", "ATAN2", "CBRT", "CEIL", "COS", "COSH", "E", "EXP", "FLOOR",
				"HYPOT", "LOG", "LOG10", "PI", "POW", "ROUND", "SIGNUM", "SIN", "SINH", "SQRT", "TAN",
				"TANH", "TAU",

				//search
				"MATCH", "QSTR",

				//spatial
				"ST_DISTANCE", "ST_INTERSECTS", "ST_DISJOINT", "ST_CONTAINS", "ST_WITHIN", "ST_X", "ST_Y",

				//string

				"BIT_LENGTH", "BYTE_LENGTH", "CONCAT", "ENDS_WITH", "FROM_BASE64", "LEFT", "LENGTH", "LOCATE",
				"LTRIM", "REPEAT", "REPLACE", "REVERSE", "RIGHT", "RTRIM", "SPACE", "SPLIT", "STARTS_WITH",
				"SUBSTRING", "TO_BASE64", "TO_LOWER", "TO_UPPER", "TRIM",

				//type conversion
				"TO_BOOLEAN", "TO_CARTESIANPOINT", "TO_CARTESIANSHAPE", "TO_DATETIME", "TO_DEGREES",
				"TO_DOUBLE", "TO_GEOPOINT", "TO_GEOSHAPE", "TO_INTEGER", "TO_IP", "TO_LONG", "TO_RADIANS",
				"TO_STRING", "TO_VERSION",

				//multivalued
				"MV_APPEND", "MV_AVG", "MV_CONCAT", "MV_COUNT", "MV_DEDUPE", "MV_FIRST", "MV_LAST", "MV_MAX",
				"MV_MEDIAN", "MV_MEDIAN_ABSOLUTE_DEVIATION", "MV_MIN", "MV_PERCENTILE", "MV_PSERIES_WEIGHTED_SUM",
				"MV_SORT", "MV_SLICE", "MV_SUM", "MV_ZIP",

				"KQL"
			]
		},
		contains: [
			hljs.QUOTE_STRING_MODE,
			hljs.C_LINE_COMMENT_MODE,
			{
				scope: "operator", // (pathname: path1/path2/dothis) color #ab5656
				match: /(?:<|<=|==|::|\w+:|!=|>=|>|LIKE|RLIKE|IS NULL|IS NOT NULL)/,
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
