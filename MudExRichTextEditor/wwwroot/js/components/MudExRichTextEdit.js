﻿class MudExRichTextEdit {
    elementRef;
    options;
    dotnet;
    quill; 
    //https://github.com/quilljs/awesome-quill?tab=readme-ov-file
    constructor(elementRef, dotNet, options) {        
        this.elementRef = elementRef;        
        this.dotnet = dotNet;
        //Quill.register("modules/htmlEditButton", htmlEditButton);

        if (window.QuillBlotFormatter && window.QuillBlotFormatter.default)
            Quill.register('modules/blotFormatter', QuillBlotFormatter.default);
      
        this.create(this.options = options);        
    }

    create(opt) {                
        var options = {
            debug: opt.debugLevel,
            modules: {                
                toolbar: opt.toolBar                              
            },
            placeholder: opt.placeholder,
            readOnly: opt.readOnly,
            theme: opt.theme
        };

        if (!opt.quillElement && !this.elementRef) {
            return;
        }

        if (opt.modules && opt.modules.length) {
            opt.modules.forEach(module => {                
                const owner = module.jsReference || window;
                const moduleConfig = module.jsConfigFunction && owner[module.jsConfigFunction] ? owner[module.jsConfigFunction](options, opt, opt.quillElement || this.elementRef ) : module.options;
                if (moduleConfig) {
                    options.modules = { ...options.modules, ...moduleConfig };
                }
            });
        }
        
        this.quill = new Quill(opt.quillElement || this.elementRef, options);
        this.quill.container.addEventListener('mouseleave', () => {
            // Invoke a method when the mouse leaves the editor
            this.dotnet.invokeMethodAsync('OnMouseLeave');
        });
        this.quill.on('text-change', (delta, oldDelta, source) => {
            var html = this.quill.root.innerHTML;
            this.dotnet.invokeMethodAsync('OnContentChanged', html, source);
        });

        this.quill.on('selection-change', (range, oldRange, source) => {
            var html = this.quill.root.innerHTML;
            if (range === null && oldRange !== null) {
                this.dotnet.invokeMethodAsync('OnBlur', html, source);
            } else if (range !== null && oldRange === null)
                this.dotnet.invokeMethodAsync('OnFocus', html, source);
        });

        if (this.quill && this.quill.root && this.quill.root.parentNode) {
            window.addEventListener('resize', this.adjustTooltipPosition.bind(this));
            this.interval = setInterval(this.adjustTooltipPosition.bind(this), 100);

            const resizeObserver = new ResizeObserver((entries) => {
                if (entries.length > 0) {
                    var height = entries[0].target.getBoundingClientRect().height;
                    this.dotnet.invokeMethodAsync('OnHeightChanged', height);
                }
            });
            resizeObserver.observe(this.quill.root.parentNode);
        }

        this.dotnet.invokeMethodAsync('OnCreated');
    }

    stopRecording() {
        if (this.recognition) {
            this.recognition.stop();
            this.recognition = null;
        }
    }

    startRecording() {
        // Überprüfen, ob der Browser die SpeechRecognition API unterstützt
        if ('webkitSpeechRecognition' in window) {
            this.recognition = new webkitSpeechRecognition();

            this.recognition.lang = 'de-DE'; // Setzen Sie die Sprache auf Deutsch
            this.recognition.continuous = true; // Kontinuierliche Erkennung
            this.recognition.interimResults = true; // Auch vorläufige Ergebnisse zurückgeben

            let isFinalResult = false;

            this.recognition.onresult = (event) => {
                let interimTranscript = '';
                for (let i = event.resultIndex; i < event.results.length; ++i) {
                    if (event.results[i].isFinal) {
                        isFinalResult = true;
                        interimTranscript += event.results[i][0].transcript + '\n'; // Fügen Sie einen Zeilenumbruch für das endgültige Ergebnis hinzu
                    } else {
                        interimTranscript += event.results[i][0].transcript;
                    }
                }

                // Ermitteln Sie die aktuelle Cursorposition
                let currentCursorPosition = this.quill.getSelection() ? this.quill.getSelection().index : this.quill.getLength();

                // Einfügen des Textes an der aktuellen Cursorposition
                if (isFinalResult) {
                    this.quill.insertText(currentCursorPosition, interimTranscript, 'user');
                    this.quill.setSelection(currentCursorPosition + interimTranscript.length);
                    isFinalResult = false;
                }
            };

            // Starten Sie die Spracherkennung
            this.recognition.start();
            return true;
        } else {
            return false;
        }
    }

    adjustTooltipPosition() {        
        var resize = window.getComputedStyle(this.quill.root.parentNode).getPropertyValue('resize');
        
        if(!resize || resize === 'none') {
            return;
        }

        var tooltip = this.elementRef.querySelector('.ql-tooltip');
        if (tooltip) {
            var leftVal = parseInt(window.getComputedStyle(tooltip).left, 10),
                topVal = parseInt(window.getComputedStyle(tooltip).top, 10);
            if (leftVal < 0) {
                tooltip.style.left = '0px';
            }
            if (topVal < 10) {
                tooltip.style.top = '10px';
            }
        }
    }

    insertImage (imageURL) {        
        var Delta = Quill.import('delta'),
            editorIndex = 0;

        if (this.quill.getSelection() !== null) {
            editorIndex = this.quill.getSelection().index;
        }

        return this.quill.updateContents(
            new Delta()
                .retain(editorIndex)
                .insert({ image: imageURL },
                    { alt: imageURL }));
    }
   

    insertMarkup(htmlMarkup) {        
        let cursorPosition = this.quill.getSelection()?.index ?? this.quill.getLength();        
        this.quill.clipboard.dangerouslyPasteHTML(cursorPosition, htmlMarkup);        
        this.quill.setSelection(cursorPosition + htmlMarkup.length);
    }
    
    dispose() {        
        clearInterval(this.interval);
        this.stopRecording();
    }
}

window.MudExRichTextEdit = MudExRichTextEdit;

export function initializeMudExRichTextEdit(elementRef, dotnet, options) {    
    return new MudExRichTextEdit(elementRef, dotnet, options);
}