import { Editor, StarterKit, Image, TextAlign, TextStyle, Color, Highlight, Placeholder } from '../../lib/tiptap/tiptap.bundle.js';

// Toolbar elements carry the Quill class names (ql-bold, ql-header, ...), so QuillTool definitions
// and custom ToolbarContent markup work unchanged. Each entry maps one class to a Tiptap command.
const commands = {
    'ql-bold': c => c.toggleBold(),
    'ql-italic': c => c.toggleItalic(),
    'ql-underline': c => c.toggleUnderline(),
    'ql-strike': c => c.toggleStrike(),
    'ql-blockquote': c => c.toggleBlockquote(),
    'ql-code-block': c => c.toggleCodeBlock(),
    'ql-clean': c => c.unsetAllMarks().clearNodes(),
    'ql-list': (c, v) => v === 'ordered' ? c.toggleOrderedList() : c.toggleBulletList(),
    // ponytail: indent only works inside lists, paragraph indent needs its own extension
    'ql-indent': (c, v) => v === '+1' ? c.sinkListItem('listItem') : c.liftListItem('listItem'),
    'ql-header': (c, v) => v ? c.toggleHeading({ level: +v }) : c.setParagraph(),
    'ql-align': (c, v) => v ? c.setTextAlign(v) : c.unsetTextAlign(),
    'ql-color': (c, v) => c.setColor(v),
    'ql-background': (c, v) => c.setHighlight({ color: v }),
};

const activeChecks = {
    'ql-bold': e => e.isActive('bold'),
    'ql-italic': e => e.isActive('italic'),
    'ql-underline': e => e.isActive('underline'),
    'ql-strike': e => e.isActive('strike'),
    'ql-blockquote': e => e.isActive('blockquote'),
    'ql-code-block': e => e.isActive('codeBlock'),
    'ql-link': e => e.isActive('link'),
    'ql-list': (e, v) => e.isActive(v === 'ordered' ? 'orderedList' : 'bulletList'),
};

const qlClass = el => [...el.classList].find(c => c.startsWith('ql-'));

class MudExTiptapEdit {
    constructor(elementRef, dotNet, options) {
        this.elementRef = elementRef;
        this.dotnet = dotNet;
        this.options = options;
        this.placeholder = options.placeholder;
        this.create(options);
    }

    create(opt) {
        const host = this.elementRef;
        if (!host) return;

        // A re-render can hand us an element that already carries an editor; drop the old one first.
        host.mudTiptapEdit?.dispose();

        // Content rendered into the host by Blazor (EditorContent) becomes the initial document.
        const initialContent = host.innerHTML;
        host.innerHTML = '';
        host.classList.add('tt-container');

        this.editor = new Editor({
            element: host,
            content: initialContent,
            editable: !opt.readOnly,
            extensions: [
                StarterKit.configure({ link: { openOnClick: false } }),
                Image.configure({ allowBase64: true }),
                TextAlign.configure({ types: ['heading', 'paragraph'] }),
                TextStyle,
                Color,
                Highlight.configure({ multicolor: true }),
                Placeholder.configure({ placeholder: () => this.placeholder ?? '' }),
                // Modules (ITiptapModule) hand in their own Tiptap extensions.
                ...(opt.modules || []).flatMap(module => module.getExtensions(this)),
            ],
            editorProps: {
                handlePaste: (view, event) => this.handleFiles(event.clipboardData?.files, undefined),
                handleDrop: (view, event, slice, moved) => !moved
                    && this.handleFiles(event.dataTransfer?.files, view.posAtCoords({ left: event.clientX, top: event.clientY })?.pos),
            },
        });
        this.host = host;
        host.mudTiptapEdit = this;

        this.editor.on('update', () => this.dotnet.invokeMethodAsync('OnContentChanged', this.editor.getHTML(), 'user'));
        this.editor.on('blur', () => this.dotnet.invokeMethodAsync('OnBlur', this.editor.getHTML(), 'user'));
        this.editor.on('focus', () => this.dotnet.invokeMethodAsync('OnFocus', this.editor.getHTML(), 'user'));
        this.editor.on('transaction', () => this.updateToolbarState());

        this.onMouseLeave = () => this.dotnet.invokeMethodAsync('OnMouseLeave');
        host.addEventListener('mouseleave', this.onMouseLeave);

        this.bindToolbar(opt.toolBar, opt);

        this.resizeObserver = new ResizeObserver(entries => {
            if (entries.length > 0) {
                this.dotnet.invokeMethodAsync('OnHeightChanged', entries[0].target.getBoundingClientRect().height);
            }
        });
        this.resizeObserver.observe(host);

        this.dotnet.invokeMethodAsync('OnCreated');
    }

    bindToolbar(toolbar, opt) {
        if (!toolbar) return;
        this.toolbar = toolbar;
        const handlerNames = opt.defaultToolHandlerNames || [];

        const run = (el) => {
            const cls = this.editor && qlClass(el);
            if (!cls) return;
            const name = cls.substring(3);
            if (handlerNames.includes(name)) {
                return this.dotnet.invokeMethodAsync('DelegateHandler', name, [el.value]);
            }
            if (cls === 'ql-link') return this.promptLink();
            if (cls === 'ql-image') return this.pickImage();
            const command = commands[cls];
            if (command) command(this.editor.chain().focus(), el.value).run();
        };

        // Keep the editor selection when a toolbar button is pressed.
        toolbar.addEventListener('mousedown', e => {
            const button = e.target.closest('button');
            if (button && qlClass(button)) e.preventDefault();
        });
        toolbar.addEventListener('click', e => {
            const button = e.target.closest('button');
            if (button && toolbar.contains(button)) run(button);
        });
        toolbar.addEventListener('change', e => {
            if (e.target.matches('select, input[type=color]')) run(e.target);
        });
    }

    updateToolbarState() {
        if (!this.toolbar || !this.editor) return;
        const e = this.editor;
        this.toolbar.querySelectorAll('button[class*="ql-"]').forEach(button => {
            const check = activeChecks[qlClass(button)];
            if (check) button.classList.toggle('ql-active', check(e, button.value));
        });
        this.toolbar.querySelectorAll('select.ql-header').forEach(select => {
            select.value = [1, 2, 3, 4, 5, 6].find(level => e.isActive('heading', { level }))?.toString() ?? '';
        });
        this.toolbar.querySelectorAll('select.ql-align').forEach(select => {
            select.value = ['center', 'right', 'justify'].find(align => e.isActive({ textAlign: align })) ?? '';
        });
    }

    // ponytail: native prompt, swap for a MudBlazor dialog when the prototype graduates
    promptLink() {
        const previous = this.editor.getAttributes('link').href;
        const url = window.prompt('URL', previous ?? 'https://');
        if (url === null) return;
        const chain = this.editor.chain().focus().extendMarkRange('link');
        (url ? chain.setLink({ href: url }) : chain.unsetLink()).run();
    }

    pickImage() {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = 'image/*';
        input.onchange = () => input.files[0] && this.uploadAndInsert(input.files[0], undefined);
        input.click();
    }

    handleFiles(files, pos) {
        if (!this.options.beforeUpload || !files || !files.length) return false;
        this.uploadAndInsert(files[0], pos);
        return true;
    }

    uploadAndInsert(file, pos) {
        const reader = new FileReader();
        reader.onload = () => {
            const fileInfo = {
                data: new Uint8Array(reader.result),
                fileName: file.name,
                extension: file.name.includes('.') ? file.name.split('.').pop() : '',
                contentType: file.type,
                path: '',
                size: file.size
            };
            this.dotnet.invokeMethodAsync('UploadImage', fileInfo)
                .then(url => {
                    const at = pos ?? this.editor.state.selection.from;
                    const node = fileInfo.contentType.indexOf('image') !== -1
                        ? { type: 'image', attrs: { src: url } }
                        : { type: 'text', text: fileInfo.fileName, marks: [{ type: 'link', attrs: { href: url } }] };
                    this.editor.chain().focus().insertContentAt(at, node).run();
                })
                .catch(error => console.error(error));
        };
        reader.readAsArrayBuffer(file);
    }

    // Engine contract shared with MudExRichTextEdit.js - the C# base class only calls these.
    getHtml() { return this.editor?.getHTML(); }
    getSemanticHtml() { return this.editor?.getHTML(); }
    getText() { return this.editor?.getText(); }
    getContent() { return JSON.stringify(this.editor?.getJSON()); }
    loadContent(content) { this.editor.commands.setContent(JSON.parse(content), { emitUpdate: false }); }

    setHtml(html) {
        this.editor.commands.setContent(html ?? '', { emitUpdate: false });
        return this.editor.getHTML();
    }

    enable(mode, placeholder) {
        this.placeholder = mode ? placeholder : '';
        this.editor?.setEditable(mode);
    }

    // ponytail: markup outside the schema (iframe, video, MudExFileDisplay wrappers) is dropped, needs custom nodes
    insertMarkup(html) { this.editor.chain().focus().insertContent(html).run(); }
    insertImage(url) { this.editor.chain().focus().setImage({ src: url }).run(); }

    startRecording(lang) {
        if (!('webkitSpeechRecognition' in window)) return false;
        this.recognition = new webkitSpeechRecognition();
        if (lang) this.recognition.lang = lang;
        this.recognition.continuous = true;
        this.recognition.interimResults = true;
        this.recognition.onresult = (event) => {
            for (let i = event.resultIndex; i < event.results.length; ++i) {
                if (event.results[i].isFinal) {
                    this.editor.chain().focus().insertContent(event.results[i][0].transcript + ' ').run();
                }
            }
        };
        this.recognition.start();
        return true;
    }

    stopRecording() {
        this.recognition?.stop();
        this.recognition = null;
    }

    dispose() {
        this.stopRecording();
        this.resizeObserver?.disconnect();
        this.resizeObserver = null;
        this.host?.removeEventListener('mouseleave', this.onMouseLeave);
        this.editor?.destroy();
        this.editor = null;
        if (this.host && this.host.mudTiptapEdit === this) {
            this.host.mudTiptapEdit = null;
        }
    }
}

export function initializeMudExTiptapEdit(elementRef, dotnet, options) {
    return new MudExTiptapEdit(elementRef, dotnet, options);
}
