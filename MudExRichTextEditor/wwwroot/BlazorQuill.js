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