// Same resolved URL as the import in MudExTiptapEdit.js, so both share one Tiptap/ProseMirror instance.
import { Mention, computePosition, flip, shift, offset } from '../lib/tiptap/tiptap.bundle.js';

// Mentions use the HTML of quill-mention (data-value, data-denotation-char, data-__data-json, data-__type),
// so content moves between MudExRichTextEdit and MudExTiptapEdit without losing them.
const quillAttribute = (html, key) => ({
    default: null,
    parseHTML: el => el.getAttribute(html),
    renderHTML: attrs => attrs[key] == null ? {} : { [html]: attrs[key] }
});

class TiptapMentionModule {
    constructor(dotnet, options) {
        this.dotnet = dotnet;
        this.options = options;
        this.selector = `span.mention[data-__type="${options.type}"]`;
    }

    // Called by MudExTiptapEdit.js before the editor is created.
    getExtensions(edit) {
        this.edit = edit;
        const module = this;
        return [Mention.extend({
            // One node type per module, so several mention modules can live in one editor.
            name: this.options.name,
            addAttributes() {
                return {
                    id: quillAttribute('data-id', 'id'),
                    label: quillAttribute('data-value', 'label'),
                    mentionSuggestionChar: quillAttribute('data-denotation-char', 'mentionSuggestionChar'),
                    dataJson: quillAttribute('data-__data-json', 'dataJson'),
                };
            },
            // Quill writes no data-type, the .NET type name identifies the module in both editors.
            parseHTML() {
                return [{ tag: module.selector }];
            },
            onCreate() {
                module.bindDomEvents(this.editor.view.dom);
            },
        }).configure({
            HTMLAttributes: { class: 'mention', 'data-__type': this.options.type },
            suggestions: this.options.denotationChars.map(char => ({
                char,
                items: async ({ query }) => (await this.dotnet.invokeMethodAsync('GetSuggestions', char, query)) ?? [],
                render: () => this.createPopup(),
            })),
        })];
    }

    bindDomEvents(dom) {
        const find = e => e.target.closest?.(this.selector);
        dom.addEventListener('click', e => {
            const el = find(e);
            if (el) this.dotnet.invokeMethodAsync('OnMentionClicked', this.toItem(this.attrsFromElement(el)));
        });
        dom.addEventListener('mouseover', e => {
            const el = find(e);
            if (el && !el.contains(e.relatedTarget)) this.dotnet.invokeMethodAsync('OnMentionHovered', this.toItem(this.attrsFromElement(el)));
        });
    }

    attrsFromElement(el) {
        return { id: el.dataset.id, label: el.dataset.value, mentionSuggestionChar: el.dataset.denotationChar, dataJson: el.dataset.__dataJson };
    }

    toItem(attrs) {
        return {
            denotationChar: attrs.mentionSuggestionChar,
            id: attrs.id,
            value: attrs.label,
            data: attrs.dataJson ? JSON.parse(attrs.dataJson) : null
        };
    }

    getMentions() {
        const result = [];
        this.edit?.editor?.state.doc.descendants(node => {
            if (node.type.name === this.options.name) result.push(this.toItem(node.attrs));
        });
        return result;
    }

    createPopup() {
        let el, props, items = [], index = 0;

        const select = (i) => {
            const item = items[i];
            if (!item) return;
            this.dotnet.invokeMethodAsync('OnBeforeSelect', item);
            props.command({ id: item.id, label: item.value, dataJson: JSON.stringify(item.data) });
            this.dotnet.invokeMethodAsync('OnAfterSelect', item);
        };

        const render = () => {
            el.replaceChildren(...items.map((item, i) => {
                const row = document.createElement('div');
                row.className = 'tt-mention-item' + (i === index ? ' selected' : '');
                row.textContent = item.value;
                // mousedown + preventDefault keeps the editor focused, a click would blur it first.
                row.addEventListener('mousedown', e => { e.preventDefault(); select(i); });
                return row;
            }));
            el.style.display = items.length ? '' : 'none';
        };

        // Fixed + floating-ui keeps the list next to the caret inside dialogs and scrolled containers.
        const position = () => {
            const rect = props.clientRect?.();
            if (!rect) return;
            computePosition({ getBoundingClientRect: () => rect }, el, {
                strategy: 'fixed',
                placement: 'bottom-start',
                middleware: [offset(4), flip(), shift({ padding: 8 })]
            }).then(({ x, y }) => Object.assign(el.style, { left: `${x}px`, top: `${y}px` }));
        };

        const update = (p) => {
            props = p;
            items = p.items ?? [];
            index = 0;
            render();
            position();
        };

        return {
            onStart: (p) => {
                el = document.createElement('div');
                el.className = 'tt-mention-list';
                document.body.appendChild(el);
                update(p);
            },
            onUpdate: update,
            onKeyDown: ({ event }) => {
                if (!items.length || el.style.display === 'none') return false;
                if (event.key === 'ArrowDown') index = (index + 1) % items.length;
                else if (event.key === 'ArrowUp') index = (index + items.length - 1) % items.length;
                else if (event.key === 'Enter' || event.key === 'Tab') { select(index); return true; }
                else if (event.key === 'Escape') { el.style.display = 'none'; return true; }
                else return false;
                render();
                return true;
            },
            onExit: () => {
                el?.remove();
                el = null;
            },
        };
    }

    dispose() {
        this.edit = null;
    }
}

export function initializeTiptapMentionModule(dotnet, options) {
    return new TiptapMentionModule(dotnet, options);
}
