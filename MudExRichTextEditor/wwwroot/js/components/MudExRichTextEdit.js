class MudExRichTextEdit {
    elementRef;
    options;
    dotnet;
    quill;
    //https://github.com/quilljs/awesome-quill?tab=readme-ov-file
    constructor(elementRef, dotNet, options) {
        this.elementRef = elementRef;
        this.dotnet = dotNet;

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
                const moduleConfig = module.jsConfigFunction && owner[module.jsConfigFunction] ? owner[module.jsConfigFunction](options, opt, opt.quillElement || this.elementRef) : module.options;
                if (moduleConfig) {
                    options.modules = { ...options.modules, ...moduleConfig };
                }
            });
        }

        this.quill = new Quill(opt.quillElement || this.elementRef, options);
        this.__quill = this.__quill || this.quill;
        opt.quillElement.__quill = this.__quill;
        opt.quillElement.quill = this.quill;
        opt.quillElement.mudRichTextEdit = this;

        if (opt.beforeUpload && !opt.defaultToolHandlerNames?.includes('image')) {
            this.quill.getModule('toolbar').addHandler('image', () => {
                const input = document.createElement('input');
                input.setAttribute('type', 'file');
                input.setAttribute('accept', 'image/*');
                input.click();

                input.onchange = (a) => {
                    const file = input.files[0];
                    if (file) {
                        const reader = new FileReader();
                        reader.onload = (e) => {
                            const arrayBuffer = reader.result;
                            const fileInfo = {
                                data: new Uint8Array(arrayBuffer),
                                fileName: file.name,
                                extension: file.name.includes('.') ? file.name.split('.').slice(-1)[0] : '',
                                contentType: file.type,
                                path: '',
                                size: file.size
                            };
                            this.dotnet.invokeMethodAsync('UploadImage', fileInfo)
                                .then(url => {
                                    const range = this.quill.getSelection();
                                    this.quill.insertEmbed(range.index, 'image', url);
                                })
                                .catch(error => console.error(error));
                        };
                        reader.readAsArrayBuffer(file);
                    }
                };
            });
        }

        if (opt.beforeUpload) {
            this.quill.root.addEventListener('paste',
                (event) => {
                    if (event.clipboardData && event.clipboardData.files && event.clipboardData.files.length > 0) {
                        const file = event.clipboardData.files[0];
                        if (file) {
                            event.preventDefault();
                            event.stopImmediatePropagation();
                            const reader = new FileReader();
                            reader.onload = () => {
                                const arrayBuffer = reader.result;
                                const fileInfo = {
                                    data: new Uint8Array(arrayBuffer),
                                    fileName: file.name,
                                    extension: file.name.includes('.') ? file.name.split('.').pop() : '',
                                    contentType: file.type,
                                    path: '',
                                    size: file.size
                                };
                                this.dotnet.invokeMethodAsync('UploadImage', fileInfo)
                                    .then(url => {
                                        const range = this.quill.getSelection() || { index: this.quill.getLength() };
                                        if(fileInfo.contentType.indexOf('image') !== -1) {
                                            this.quill.insertEmbed(range.index, 'image', url);
                                        }
                                        else {
                                            this.quill.insertText(range.index, fileInfo.fileName, 'user');
                                            this.quill.setSelection(range.index, fileInfo.fileName.length);
                                            this.quill.theme.tooltip.edit('link', url);
                                            this.quill.theme.tooltip.save();
                                        }
                                    })
                                    .catch(error => console.error(error));
                            };
                            reader.readAsArrayBuffer(file);
                        }
                    }
                }, true);
        }

        if (opt.defaultToolHandlerNames) {
            opt.defaultToolHandlerNames.forEach(handler => {
                this.quill.getModule('toolbar').addHandler(handler, () => {
                    return this.dotnet.invokeMethodAsync('DelegateHandler', handler, [...arguments].slice(1));
                });
            });
        }

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

            
            const intersectionObserver = new IntersectionObserver((entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting && entry.intersectionRatio > 0) {
                        // Element has become visible, update Quill's layout
                        if (this.quill && this.quill.root) {
                            // Force Quill to recalculate its bounds and update its layout
                            this.quill.root.style.height = '';
                            // Trigger a resize event to ensure proper dimensions
                            setTimeout(() => {
                                if (this.quill && this.quill.root && this.quill.root.parentNode) {                                    
                                    const height = null; //this.quill.root.parentNode.getBoundingClientRect().height;
                                    this.dotnet.invokeMethodAsync('OnHeightChanged', height);
                                }
                            }, 50);
                        }
                    }
                });
            }, { threshold: [0, 0.1] });
            
            this.intersectionObserver = intersectionObserver;
            intersectionObserver.observe(this.quill.root.parentNode);
        }

        this.dotnet.invokeMethodAsync('OnCreated');
    }

    stopRecording() {
        if (this.recognition) {
            this.recognition.stop();
            this.recognition = null;
        }
    }

    startRecording(lang) {
        if ('webkitSpeechRecognition' in window) {
            this.recognition = new webkitSpeechRecognition();

            if (lang) {                
                this.recognition.lang = lang;
            }
            this.recognition.continuous = true;
            this.recognition.interimResults = true;

            let isFinalResult = false;

            this.recognition.onresult = (event) => {
                let interimTranscript = '';
                for (let i = event.resultIndex; i < event.results.length; ++i) {
                    if (event.results[i].isFinal) {
                        isFinalResult = true;
                        interimTranscript += event.results[i][0].transcript + '\n';
                    } else {
                        interimTranscript += event.results[i][0].transcript;
                    }
                }

                let currentCursorPosition = this.quill.getSelection() ? this.quill.getSelection().index : this.quill.getLength();

                if (isFinalResult) {
                    this.quill.insertText(currentCursorPosition, interimTranscript, 'user');
                    this.quill.setSelection(currentCursorPosition + interimTranscript.length);
                    isFinalResult = false;
                }
            };

            this.recognition.start();
            return true;
        } else {
            return false;
        }
    }

    adjustTooltipPosition() {
        var resize = window.getComputedStyle(this.quill.root.parentNode).getPropertyValue('resize');

        if (!resize || resize === 'none') {
            return;
        }
        
        this.adjustPos(this.elementRef.querySelector('.ql-tooltip'));
        this.adjustPos(this.elementRef.querySelector('.ql-cell-properties-form'));
        this.adjustPos(this.elementRef.querySelector('.ql-table-properties-form'));
    }

    adjustPos(element) {
        if (!element) return;
        if (element.dataset.mouseIsDown) {
            return;
        }
        const computedStyle = window.getComputedStyle(element);
        const position = computedStyle.position;
        if (position === "absolute" || position === "fixed") {
            const parent = element.parentElement;
            if (parent) {
                const parentRect = parent.getBoundingClientRect();
                const elRect = element.getBoundingClientRect();
                if (
                    elRect.left < parentRect.left ||
                    elRect.top < parentRect.top ||
                    elRect.right > parentRect.right ||
                    elRect.bottom > parentRect.bottom
                ) {
                    element.style.left = "0";
                }
            }
        }

        if (!element.dataset.dragAttached) {
            element.dataset.dragAttached = "true";

            element.addEventListener("mousedown", function onMouseDown(e) {

                const computedStyle = window.getComputedStyle(element);
                const resizeProp = computedStyle.resize;
                const threshold = 10; // Pixel als Schwellwert
                let isResizing = false;
                if (resizeProp !== "none") {
                    // Wenn horizontales oder beides (horizontal + vertikal) Resizing aktiv ist,
                    // prüfen wir, ob der Klick nahe dem rechten Rand liegt.
                    if ((resizeProp === "both" || resizeProp === "horizontal") &&
                        (e.offsetX > element.offsetWidth - threshold)) {
                        isResizing = true;
                    }
                    // Wenn vertikales oder beides aktiv ist, prüfen wir den unteren Rand.
                    if ((resizeProp === "both" || resizeProp === "vertical") &&
                        (e.offsetY > element.offsetHeight - threshold)) {
                        isResizing = true;
                    }
                }
                // Falls der Klick im Resize-Bereich erfolgte, nichts weiter machen – native Resize bleibt aktiv.
                if (isResizing) {
                    return;
                }

                element.dataset.mouseIsDown = true;
                e.preventDefault();

                // Start der Positionsberechnung
                const startX = e.clientX;
                const startY = e.clientY;
                const initialLeft = parseInt(window.getComputedStyle(element).left, 10) || 0;
                const initialTop = parseInt(window.getComputedStyle(element).top, 10) || 0;
                const offsetX = startX - initialLeft;
                const offsetY = startY - initialTop;

                function onMouseMove(e) {
                    let newLeft = e.clientX - offsetX;
                    let newTop = e.clientY - offsetY;

                    // Optional: Begrenzung innerhalb des Parent-Elements
                    if (element.parentElement) {
                        const parentRect = element.parentElement.getBoundingClientRect();
                        const elRect = element.getBoundingClientRect();
                        const elementWidth = elRect.width;
                        const elementHeight = elRect.height;

                        if (newLeft < 0) newLeft = 0;
                        if (newTop < 0) newTop = 0;
                        if (newLeft + elementWidth > parentRect.width) {
                            newLeft = parentRect.width - elementWidth;
                        }
                        if (newTop + elementHeight > parentRect.height) {
                            newTop = parentRect.height - elementHeight;
                        }
                    }
                    element.style.left = newLeft + "px";
                    element.style.top = newTop + "px";
                }

                function onMouseUp() {
                    element.dataset.mouseIsDown = false;
                    document.removeEventListener("mousemove", onMouseMove);
                    document.removeEventListener("mouseup", onMouseUp);
                }

                document.addEventListener("mousemove", onMouseMove);
                document.addEventListener("mouseup", onMouseUp);
            });
        }
    }

    insertImage(imageURL) {
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
        if (this.intersectionObserver) {
            this.intersectionObserver.disconnect();
            this.intersectionObserver = null;
        }
    }
}

window.MudExRichTextEdit = MudExRichTextEdit;

export function initializeMudExRichTextEdit(elementRef, dotnet, options) {
    return new MudExRichTextEdit(elementRef, dotnet, options);
}