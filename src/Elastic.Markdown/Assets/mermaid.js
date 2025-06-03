var mermaidInitialize = function () {
    mermaid.initialize({
        startOnLoad: false, theme: 'base',
        themeVariables: {
            fontFamily: 'inherit',
            altFontFamily: 'inherit',
            fontSize: '0.875em',
        },
        fontFamily: 'inherit', altFontFamily: 'inherit',
        "sequence": {
            "actorFontFamily": "inherit",
            "noteFontFamily": "inherit",
            "messageFontFamily": "inherit"
        },
        "journey": {
            "taskFontFamily": "inherit"
        }
    });
    mermaid.run({
        nodes: document.querySelectorAll('.mermaid'),
    });
}

document.addEventListener('htmx:load', function () {
    mermaidInitialize()
})
