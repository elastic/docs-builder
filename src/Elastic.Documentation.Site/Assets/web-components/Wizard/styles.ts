// Styles live in the shadow root so site CSS can't leak in and the widget can't leak out.
export const styles = `
:host {
  --wizard-accent: #0b64dd; --wizard-accent-tint: #eaf2fd; --wizard-ink: #1a1c21; --wizard-muted: #5a6275;
  --wizard-line: #d9dee8; --wizard-surface: #fff; --wizard-ok: #0a7d5a; --wizard-ok-tint: #e3f6ef;
  --wizard-warn: #fff8e6; --wizard-warn-line: #f0d28a; --wizard-radius: 14px;
  display: block; margin: 1.5rem 0; font: inherit; color: var(--wizard-ink);
}
* { box-sizing: border-box; }
.card { background: var(--wizard-surface); border: 1px solid var(--wizard-line); border-radius: var(--wizard-radius); padding: 28px; box-shadow: 0 2px 10px rgba(20,30,60,.06); }
.progress { height: 8px; background: var(--wizard-line); border-radius: 99px; overflow: hidden; margin-bottom: 8px; }
.progress > div { height: 100%; background: var(--wizard-accent); transition: width .3s ease; }
.progress-label { font-size: 13px; color: var(--wizard-muted); margin-bottom: 20px; display: flex; justify-content: space-between; }
.topbar { display: flex; justify-content: space-between; min-height: 28px; margin-bottom: 8px; }
.link { background: none; border: 0; padding: 4px 0; color: var(--wizard-accent); font: inherit; cursor: pointer; }
.link:hover { text-decoration: underline; }
h2.title { font-size: 26px; line-height: 1.25; margin: 0 0 8px; border: 0; padding: 0; }
h2.title:focus { outline: none; }
.why { background: var(--wizard-accent-tint); border-radius: 10px; padding: 10px 14px; margin: 12px 0 20px; font-size: 14px; color: #23406b; }
.options { display: grid; gap: 10px; margin: 0; padding: 0; list-style: none; }
.option { width: 100%; text-align: left; background: var(--wizard-surface); border: 2px solid var(--wizard-line); border-radius: 12px; padding: 14px 16px; font: inherit; color: inherit; cursor: pointer; display: flex; gap: 12px; align-items: flex-start; transition: border-color .12s, background .12s; }
.option:hover { border-color: var(--wizard-accent); background: var(--wizard-accent-tint); }
.option[aria-pressed="true"] { border-color: var(--wizard-accent); background: var(--wizard-accent-tint); }
.option .box { flex: 0 0 20px; height: 20px; margin-top: 2px; border: 2px solid var(--wizard-line); border-radius: 5px; background: var(--wizard-surface); }
.option[aria-pressed="true"] .box { background: var(--wizard-accent); border-color: var(--wizard-accent); box-shadow: inset 0 0 0 3px #fff; }
.options.cards { grid-template-columns: repeat(auto-fit, minmax(210px, 1fr)); }
.options.cards li.wide { grid-column: 1 / -1; }
.options.cards .option { height: 100%; padding: 18px; }
.options.cards .option .label { font-size: 17px; }
.options.cards .option .hint { margin-top: 6px; }
.tags { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 12px; }
.tag { font-size: 12px; font-weight: 500; color: var(--wizard-muted); background: var(--wizard-surface); border: 1px solid var(--wizard-line); border-radius: 99px; padding: 2px 10px; }
.option .label { font-weight: 600; }
.option .hint { display: block; font-weight: 400; font-size: 14px; color: var(--wizard-muted); }
button:focus-visible, a:focus-visible { outline: 3px solid #7fb0f5; outline-offset: 2px; }
.actions { display: flex; gap: 12px; align-items: center; margin-top: 20px; }
.btn { display: inline-block; background: var(--wizard-accent); color: #fff; border: 0; border-radius: 10px; padding: 12px 22px; font: inherit; font-weight: 600; cursor: pointer; text-decoration: none; }
.btn:hover { filter: brightness(1.08); }
.btn[disabled] { opacity: .5; cursor: not-allowed; }
.hint-text { color: var(--wizard-muted); font-size: 14px; }
.intro p { color: var(--wizard-muted); font-size: 17px; margin: 8px 0 24px; }
.badge { display: inline-block; font-size: 13px; font-weight: 600; color: var(--wizard-ok); background: var(--wizard-ok-tint); border-radius: 99px; padding: 3px 12px; margin-bottom: 10px; }
.blurbs { color: var(--wizard-muted); margin: 4px 0 18px; }
.blurbs p { margin: 0 0 4px; }
.note { background: var(--wizard-warn); border: 1px solid var(--wizard-warn-line); border-radius: 10px; padding: 10px 14px; margin: 8px 0; font-size: 14px; }
h3 { font-size: 15px; text-transform: uppercase; letter-spacing: .04em; color: var(--wizard-muted); margin: 26px 0 10px; }
.docs { display: grid; grid-template-columns: repeat(auto-fit, minmax(190px, 1fr)); gap: 10px; }
.doc { display: block; border: 1px solid var(--wizard-line); border-radius: 10px; padding: 12px 14px; color: inherit; text-decoration: none; background: var(--wizard-surface); }
.doc:hover { border-color: var(--wizard-accent); background: var(--wizard-accent-tint); }
.doc small { display: block; color: var(--wizard-accent); font-weight: 600; margin-bottom: 2px; }
.answers { margin-top: 26px; border: 1px solid var(--wizard-line); border-radius: 10px; }
.answers summary { cursor: pointer; padding: 12px 14px; font-weight: 600; font-size: 15px; color: var(--wizard-muted); list-style: none; display: flex; justify-content: space-between; align-items: center; }
.answers summary::-webkit-details-marker { display: none; }
.answers summary::after { content: ''; width: 8px; height: 8px; border-right: 2px solid var(--wizard-muted); border-bottom: 2px solid var(--wizard-muted); transform: rotate(45deg); transition: transform .15s; margin-right: 4px; }
.answers[open] summary::after { transform: rotate(-135deg); }
.answers summary:hover { background: #f5f7fa; border-radius: 10px; }
.recap { margin: 0; padding: 0; list-style: none; border-top: 1px solid var(--wizard-line); }
.recap li { display: flex; justify-content: space-between; gap: 12px; padding: 10px 14px; border-top: 1px solid var(--wizard-line); font-size: 14px; }
.recap li:first-child { border-top: 0; }
.recap .q { color: var(--wizard-muted); }
.recap .a { font-weight: 600; }
.option[data-previous="true"] { border-color: var(--wizard-accent); }
.option .was { display: inline-block; margin-top: 4px; font-size: 12px; font-weight: 600; color: var(--wizard-accent); }
.error { color: #a4161a; }
@media (max-width: 480px) { .card { padding: 20px; } h2.title { font-size: 22px; } }
@media (prefers-reduced-motion: reduce) { * { transition: none !important; } }
`
