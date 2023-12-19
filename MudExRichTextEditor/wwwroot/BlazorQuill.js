(function () {
    window.QuillFunctions = {
        create: function (opt) {

            var options = {
                debug: opt.debugLevel,
                modules: {
                    toolbar: opt.toolBar
                },
                placeholder: opt.placeholder,
                readOnly: opt.readOnly,
                theme: opt.theme
            };

            var quill = new Quill(opt.quillElement, options);
            quill.on('text-change', function (delta, oldDelta, source) {
                var html = quill.root.innerHTML;
                opt.dotnet.invokeMethodAsync('OnContentChanged', html, source);
            });
            quill.on('selection-change', function (range, oldRange, source) {
                var html = quill.root.innerHTML;
                if (range === null && oldRange !== null) {
                    opt.dotnet.invokeMethodAsync('OnBlur', html, source);
                } else if (range !== null && oldRange === null)
                    opt.dotnet.invokeMethodAsync('OnFocus', html, source);
            });

            var adjustTooltipPosition = function () {
                var tooltip = document.querySelector('.ql-tooltip');
                if (tooltip) {
                    var leftVal = parseInt(window.getComputedStyle(tooltip).left, 10);
                    if (leftVal < 0) {
                        tooltip.style.left = '0px';
                    }
                }
            }

            window.addEventListener('resize', adjustTooltipPosition);
            setInterval(adjustTooltipPosition, 500);

        },

        insertImage: function (quillElement, imageURL) {
            var Delta = Quill.import('delta');
            editorIndex = 0;

            if (quillElement.__quill.getSelection() !== null) {
                editorIndex = quillElement.__quill.getSelection().index;
            }

            return quillElement.__quill.updateContents(
                new Delta()
                    .retain(editorIndex)
                    .insert({ image: imageURL },
                        { alt: imageURL }));
        }
    };
})();


class MudExRichTextEdit {
    elementRef;
    options;
    dotnet;
    canvas;
    _canvasContainer;
    _previewControl;
    color;
    _el;

    constructor(elementRef, canvasContainer, dotNet, options) {
        debugger;
        this.elementRef = elementRef;
        // this.elementRef.onclick = this._onClick.bind(this);
        this._canvasContainer = canvasContainer;
        this.dotnet = dotNet;
        this.setOptions(options);
    }
    
    dispose() {
        document.body.removeEventListener("mousedown", this._onBodyClick.bind(this));
    }
}

window.MudExColorBubble = MudExColorBubble;

window.initializeMudExRichTextEdit = function (elementRef, canvasContainer, dotnet, options) {
    return new MudExRichTextEdit(elementRef, canvasContainer, dotnet, options);
}