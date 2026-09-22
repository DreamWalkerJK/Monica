import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

const source = await readFile(new URL('../../../Monica.Framework.UI/wwwroot/js/logging-ui.js', import.meta.url), 'utf8');
const { downloadFile } = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

// These tests own browser boundaries only; they do not emulate browser download policy.
function browser(t, { response, fetchError, clickError, hasHost = true } = {}) {
    const requests = [];
    const downloads = [];
    const anchors = [];
    const activeUrls = new Map();
    const revokedUrls = [];
    const timers = [];
    const host = {
        appendChild(anchor) {
            anchor.isConnected = true;
        }
    };
    const previousDocument = Object.getOwnPropertyDescriptor(globalThis, 'document');
    Object.defineProperty(globalThis, 'document', {
        configurable: true,
        value: {
            body: hasHost ? host : null,
            documentElement: hasHost ? host : null,
            createElement(tagName) {
                assert.equal(tagName, 'a');
                const anchor = {
                    style: {},
                    isConnected: false,
                    click() {
                        if (clickError) {
                            throw clickError;
                        }

                        downloads.push({
                            href: this.href,
                            filename: this.download,
                            blob: activeUrls.get(this.href)
                        });
                    },
                    remove() {
                        this.isConnected = false;
                    }
                };
                anchors.push(anchor);
                return anchor;
            }
        }
    });
    t.after(() => {
        if (previousDocument) {
            Object.defineProperty(globalThis, 'document', previousDocument);
        } else {
            delete globalThis.document;
        }
    });
    t.mock.method(globalThis, 'fetch', async (url, options) => {
        requests.push({ url, options });
        if (fetchError) {
            throw fetchError;
        }

        return response ?? new Response('log content');
    });
    t.mock.method(URL, 'createObjectURL', blob => {
        const url = `blob:logging-test/${anchors.length + activeUrls.size}`;
        activeUrls.set(url, blob);
        return url;
    });
    t.mock.method(URL, 'revokeObjectURL', url => {
        revokedUrls.push(url);
        activeUrls.delete(url);
    });
    t.mock.method(globalThis, 'setTimeout', callback => {
        timers.push(callback);
        return timers.length;
    });

    return {
        requests,
        downloads,
        anchors,
        activeUrls,
        revokedUrls,
        runTimers() {
            while (timers.length > 0) {
                timers.shift()();
            }
        }
    };
}

for (const url of [
    'http://logging.example.test/logging-ui/current/export',
    'http://logging.example.test/services/operations/logging-ui/files/archive/%E6%97%A5%E5%BF%97%20%231.log'
]) {
    test(`DownloadFile_WhenRequested_ShouldSaveCompleteBlobWithoutNavigatingToHttp: ${url}`, async t => {
        const bytes = Buffer.concat([
            Buffer.from('\ufeff日志开始\r\n'),
            Buffer.from(Array.from({ length: 131072 }, (_, index) => index % 256)),
            Buffer.from('\r\n日志结束')
        ]);
        const filename = '运行日志 2026-09-18.log';
        const surface = browser(t, {
            response: new Response(bytes, {
                headers: {
                    'Content-Type': 'text/plain; charset=utf-8',
                    'Content-Disposition': `attachment; filename="fallback.log"; filename*=UTF-8''${encodeURIComponent(filename)}`
                }
            })
        });

        assert.equal(await downloadFile(url), true);

        assert.equal(surface.requests.length, 1);
        assert.equal(surface.requests[0].url, url, 'The base path and escaped filename must reach the server unchanged');
        assert.equal(surface.requests[0].options.credentials, 'same-origin');
        assert.equal(surface.requests[0].options.mode, 'same-origin');
        assert.equal(surface.requests[0].options.redirect, 'error');
        assert.equal(surface.downloads.length, 1);
        const download = surface.downloads[0];
        assert.match(download.href, /^blob:/, 'The downloadable link must reference local bytes instead of the HTTP endpoint');
        assert.equal(download.filename, filename);
        assert.ok(download.blob instanceof Blob, 'The object URL must still be valid when the browser receives the click');
        assert.deepEqual(Buffer.from(await download.blob.arrayBuffer()), bytes);
        assert.ok(surface.anchors.every(anchor => !anchor.isConnected));

        surface.runTimers();
        assert.equal(surface.activeUrls.size, 0);
        assert.deepEqual(surface.revokedUrls, [download.href]);
    });
}

for (const [disposition, filename] of [
    ['attachment; filename="log snapshot.log"', 'log snapshot.log'],
    ['attachment; filename=log-0.log', 'log-0.log'],
    ['attachment; filename="fallback.log"; filename*=UTF-8\'\'%E6%ZZ.log', 'fallback.log'],
    [null, 'logs.log']
]) {
    test(`DownloadFile_WhenDispositionIs${disposition ?? 'Missing'}_ShouldUseFilename: ${filename}`, async t => {
        const surface = browser(t, {
            response: new Response('content', {
                headers: disposition ? { 'Content-Disposition': disposition } : {}
            })
        });

        assert.equal(await downloadFile('/logging-ui/files/log-0.log'), true);
        assert.equal(surface.downloads[0].filename, filename);
        surface.runTimers();
    });
}

for (const status of [404, 500]) {
    test(`DownloadFile_WhenHttpStatusIs${status}_ShouldRejectWithoutSavingErrorBody`, async t => {
        const surface = browser(t, { response: new Response('server error', { status }) });

        await assert.rejects(downloadFile('/logging-ui/current/export'), new RegExp(String(status)));

        assert.equal(surface.downloads.length, 0);
        assert.equal(surface.activeUrls.size, 0);
        assert.equal(surface.anchors.length, 0);
    });
}

test('DownloadFile_WhenFetchRejectsRedirect_ShouldRejectWithoutSavingAFile', async t => {
    const failure = new TypeError('Redirect rejected');
    const surface = browser(t, { fetchError: failure });

    await assert.rejects(downloadFile('/logging-ui/files/log-0.log'), failure);

    assert.equal(surface.requests[0].options.redirect, 'error');
    assert.equal(surface.downloads.length, 0);
    assert.equal(surface.activeUrls.size, 0);
    assert.equal(surface.anchors.length, 0);
});

test('DownloadFile_WhenBodyReadFails_ShouldRejectWithoutSavingPartialContent', async t => {
    const failure = new Error('Connection closed while receiving logs');
    const surface = browser(t, {
        response: {
            ok: true,
            headers: new Headers(),
            async blob() { throw failure; }
        }
    });

    await assert.rejects(downloadFile('/logging-ui/current/export'), failure);

    assert.equal(surface.downloads.length, 0);
    assert.equal(surface.activeUrls.size, 0);
});

test('DownloadFile_WhenClickFails_ShouldRemoveAnchorAndReleaseBlob', async t => {
    const failure = new Error('Download click failed');
    const surface = browser(t, { clickError: failure });

    await assert.rejects(downloadFile('/logging-ui/current/export'), failure);
    surface.runTimers();

    assert.equal(surface.downloads.length, 0);
    assert.ok(surface.anchors.length > 0);
    assert.ok(surface.anchors.every(anchor => !anchor.isConnected));
    assert.equal(surface.activeUrls.size, 0);
    assert.equal(surface.revokedUrls.length, 1);
});

test('DownloadFile_WhenUrlIsMissing_ShouldReturnFalseWithoutFetching', async t => {
    const surface = browser(t);

    assert.equal(await downloadFile(''), false);
    assert.equal(await downloadFile(null), false);
    assert.equal(surface.requests.length, 0);
    assert.equal(surface.downloads.length, 0);
});

test('DownloadFile_WhenDomHostIsMissing_ShouldReturnFalseWithoutFetching', async t => {
    const surface = browser(t, { hasHost: false });

    assert.equal(await downloadFile('/logging-ui/current/export'), false);
    assert.equal(surface.requests.length, 0);
    assert.equal(surface.downloads.length, 0);
});
