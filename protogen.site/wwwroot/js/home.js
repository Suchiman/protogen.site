var monacoInterop = {};
monacoInterop.editors = {};
monacoInterop.initializeAsync = function initializeAsync() {
    return new Promise((resolve, reject) => {
        require.config({ paths: { 'vs': 'monaco-editor/min/vs' } });
        require(['vs/editor/editor.main', 'js/proto3lang'], function (_, proto3lang) {
            monaco.languages.register({ id: 'proto3lang' });
            monaco.languages.setMonarchTokensProvider('proto3lang', proto3lang);
            resolve();
        });
    });
};
monacoInterop.createEditor = function createEditor(elementId, initialCode, language, readOnly) {
    var editor = monaco.editor.create(document.getElementById(elementId), {
        value: initialCode,
        language: language,
        readOnly: readOnly
    });
    monacoInterop.editors[elementId] = editor;
};
monacoInterop.getCode = function getCode(elementId) {
    return monacoInterop.editors[elementId].getValue({ preserveBOM: false, lineEnding: "\n" });
};
monacoInterop.setCode = function setCode(elementId, code) {
    monacoInterop.editors[elementId].setValue(code);
};
window.monacoInterop = monacoInterop;

var oldDecorations = [];
window.processResults = function processResults(data) {
    var editor = monacoInterop.editors["protocontainer"];
    var decorations = [];
    if (data.parserExceptions) {
        for (var i = 0; i < data.parserExceptions.length; i++) {
            var parserException = data.parserExceptions[i];
            decorations.push({
                range: new monaco.Range(parserException.lineNumber, parserException.columnNumber, parserException.lineNumber, parserException.columnNumber + parserException.text.length),
                options: {
                    inlineClassName: parserException.isError ? "redsquiggly" : "greensquiggly",
                    hoverMessage: parserException.message,
                    overviewRuler: {
                        color: parserException.isError ? "#E47777" : "#71B771",
                        position: parserException.isError ? monaco.editor.OverviewRulerLane.Right : monaco.editor.OverviewRulerLane.Center
                    }
                }
            });
        }
    }
    if (data.exception) {
        decorations.push({
            range: new monaco.Range(1, 1, editor.getModel().getLineCount(), 1),
            options: {
                isWholeLine: true,
                inlineClassName: "redsquiggly",
                hoverMessage: data.exception
            }
        });
    }
    oldDecorations = editor.deltaDecorations(oldDecorations, decorations);
};