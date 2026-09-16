import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

const source = await readFile(new URL('../../../Monica.UI/wwwroot/js/mo-markdown-mermaid.js', import.meta.url), 'utf8');
const { renderMermaid } = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

function diagramSurface(bounds, viewBox) {
    const attributes = new Map([['viewBox', viewBox], ['height', '2000']]);
    let inserted = false;
    const svg = {
        style: {},
        getBBox() {
            assert.ok(inserted, 'Geometry must be measured after the SVG enters its final container');
            return bounds;
        },
        setAttribute: (name, value) => attributes.set(name, value),
        removeAttribute: name => attributes.delete(name)
    };
    const element = {
        set innerHTML(value) { inserted = value.length > 0; },
        querySelector: () => svg
    };
    return { element, svg, attributes };
}

test('rendered diagrams fit their content when staging bounds are oversized or clip nodes', async () => {
    let config;
    let boundElement;
    const mermaid = {
        initialize(value) { config = value; },
        async render() { return { svg: '<svg></svg>', bindFunctions: element => { boundElement = element; } }; }
    };
    const previousWindow = globalThis.window;
    globalThis.window = { mermaid };
    try {
        for (const initialViewBox of ['-138 -47 2146 2055', '-134 -42 1003 92']) {
            const bounds = { x: 8, y: 8, width: 1105, height: 68 };
            const { element, svg, attributes } = diagramSurface(bounds, initialViewBox);
            await renderMermaid(element, 'flowchart LR\nA-->B', { fontFamily: ['sans-serif'] });

            const [x, y, width, height] = attributes.get('viewBox').split(' ').map(Number);
            assert.ok(x < bounds.x && y < bounds.y);
            assert.ok(x + width > bounds.x + bounds.width && y + height > bounds.y + bounds.height);
            assert.ok(width < bounds.width + 40 && height < bounds.height + 40, 'Canvas must not retain the temporary blank area');
            assert.equal(attributes.get('width'), '100%');
            assert.equal(attributes.has('height'), false);
            assert.equal(Number.parseFloat(svg.style.maxWidth), width);
            assert.equal(config.htmlLabels, false);
            assert.equal(config.flowchart.htmlLabels, false);
            assert.equal(config.securityLevel, 'strict');
            assert.equal(boundElement, element);
        }
    } finally {
        globalThis.window = previousWindow;
    }
});

test('an empty or invalid measurement does not replace the renderer viewport', async () => {
    const previousWindow = globalThis.window;
    globalThis.window = { mermaid: { initialize() {}, async render() { return { svg: '<svg></svg>' }; } } };
    try {
        for (const bounds of [{ x: 0, y: 0, width: 0, height: 0 }, { x: 0, y: 0, width: NaN, height: 50 }]) {
            const { element, attributes } = diagramSurface(bounds, '0 0 100 50');
            await renderMermaid(element, 'flowchart LR\nA-->B', { fontFamily: ['sans-serif'] });
            assert.equal(attributes.get('viewBox'), '0 0 100 50');
        }
    } finally {
        globalThis.window = previousWindow;
    }
});
