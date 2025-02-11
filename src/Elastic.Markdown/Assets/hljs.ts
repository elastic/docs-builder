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

hljs.addPlugin(mergeHTMLPlugin);

export function initHighlight() {
	hljs.highlightAll();
}
