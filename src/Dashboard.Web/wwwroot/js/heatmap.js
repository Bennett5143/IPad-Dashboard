// Lauf-Heatmap im Stil von Sam Wilsons "running-heatmap" / Stravas Personal Heatmap.
// Zeichnet jede Aktivität als zusammenhängende Linie auf dunkler Karte. Mehrere Ebenen:
//   heat       – Häufigkeit (additive Überlagerung, leuchtet wo oft gelaufen)
//   pace       – Geschwindigkeit je Segment (heller = schneller)
//   elevation  – Steilheit je Segment (heller = steiler)
//   direction  – Laufrichtung (lila = bergauf, grün = bergab)
//   heartrate  – Herzfrequenz je Segment (blau = niedrig, rot = hoch)
// Gerendert auf einem Canvas-Overlay (performant, additive Überlagerung). Daten kommen pro Lauf
// aus Blazor: { pts:[[lat,lng]...], t:[s]|null, alt:[m]|null, hr:[bpm]|null } (server-seitig gedünnt).

let leafletLoader = null;
let HeatCanvasLayer = null;

function loadScript(src) {
    return new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = src;
        script.async = true;
        script.onload = resolve;
        script.onerror = () => reject(new Error('Konnte Skript nicht laden: ' + src));
        document.head.appendChild(script);
    });
}

function addStylesheet(href) {
    return new Promise(resolve => {
        if (document.querySelector(`link[href="${href}"]`)) {
            resolve();
            return;
        }
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = href;
        link.onload = () => resolve();
        link.onerror = () => resolve(); // CSS-Fehler nicht den Map-Aufbau blockieren lassen
        document.head.appendChild(link);
    });
}

function ensureLeaflet() {
    if (window.L) {
        return Promise.resolve();
    }
    if (leafletLoader) {
        return leafletLoader;
    }
    // Leaflet wird lokal ausgeliefert (wwwroot/lib/leaflet), NICHT vom CDN – sonst rendert die
    // Karte z. B. auf dem Kiosk-iPad gar nicht, wenn unpkg/CDN nicht erreichbar oder geblockt ist.
    // WICHTIG: auch auf das CSS warten – ohne Leaflet-Styles bleibt der erste Aufbau schwarz.
    leafletLoader = Promise.all([
        addStylesheet('/lib/leaflet/leaflet.css'),
        loadScript('/lib/leaflet/leaflet.js')
    ]);
    return leafletLoader;
}

// Wartet, bis der Container tatsächlich Maße hat. Das scoped CSS (height:70vh) greift u. U.
// minimal nach dem ersten Render – Leaflet auf einem 0×0-Container erzeugt eine schwarze Karte,
// die invalidateSize nicht zuverlässig heilt. Also erst bei echter Größe die Karte bauen.
function waitForSize(element) {
    return new Promise(resolve => {
        let tries = 0;
        const check = () => {
            if ((element.clientWidth > 0 && element.clientHeight > 0) || tries++ > 60) {
                resolve();
            } else {
                requestAnimationFrame(check);
            }
        };
        check();
    });
}

// ---- Geometrie / Farben -------------------------------------------------

function haversine(a, b) {
    const R = 6371000, rad = Math.PI / 180;
    const dLat = (b[0] - a[0]) * rad, dLng = (b[1] - a[1]) * rad;
    const la1 = a[0] * rad, la2 = b[0] * rad;
    const h = Math.sin(dLat / 2) ** 2 + Math.cos(la1) * Math.cos(la2) * Math.sin(dLng / 2) ** 2;
    return 2 * R * Math.asin(Math.min(1, Math.sqrt(h)));
}

function clamp(v, lo, hi) { return Math.max(lo, Math.min(hi, v)); }
function lerpArr(a, b, t) { return [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t]; }
function rgb(c) { return `rgb(${c[0] | 0},${c[1] | 0},${c[2] | 0})`; }

// Drei-Stopp-Verlauf 0..1
function ramp(x, a, b, c) {
    return x < 0.5 ? rgb(lerpArr(a, b, x * 2)) : rgb(lerpArr(b, c, (x - 0.5) * 2));
}
const warmColor = x => ramp(x, [120, 60, 20], [255, 140, 40], [255, 232, 150]);  // dunkel → amber → hell (auf dunkler Karte sichtbar)
const hrColor = x => ramp(x, [44, 127, 184], [255, 207, 51], [226, 59, 59]);     // blau → gelb → rot

function directionColor(signedGradient, scale) {
    const t = clamp(signedGradient / (scale || 1), -1, 1);
    const grey = [170, 176, 182];
    return t >= 0 ? rgb(lerpArr(grey, [160, 92, 255], t))   // bergauf → lila
        : rgb(lerpArr(grey, [60, 208, 112], -t));            // bergab → grün
}

function robustRange(values) {
    const s = [...values].sort((a, b) => a - b);
    const lo = s[Math.floor(s.length * 0.05)];
    const hi = s[Math.floor(s.length * 0.95)];
    return hi > lo ? [lo, hi] : [s[0], (s[s.length - 1] ?? s[0]) + 1];
}

// Skalare je Segment (Länge pts-1) für die gewählte Ebene; null wenn der nötige Stream fehlt.
function segmentValues(run, layer) {
    const pts = run.pts, n = pts.length, out = [];
    if (layer === 'pace') {
        if (!run.t) return null;
        for (let i = 0; i < n - 1; i++) {
            const dt = run.t[i + 1] - run.t[i], d = haversine(pts[i], pts[i + 1]);
            out.push(dt > 0 && d > 0 ? d / dt : null); // Geschwindigkeit m/s
        }
    } else if (layer === 'elevation') {
        if (!run.alt) return null;
        for (let i = 0; i < n - 1; i++) {
            const d = haversine(pts[i], pts[i + 1]);
            out.push(d > 0 ? Math.abs(run.alt[i + 1] - run.alt[i]) / d : null);
        }
    } else if (layer === 'direction') {
        if (!run.alt) return null;
        for (let i = 0; i < n - 1; i++) {
            const d = haversine(pts[i], pts[i + 1]);
            out.push(d > 0 ? (run.alt[i + 1] - run.alt[i]) / d : 0);
        }
    } else if (layer === 'heartrate') {
        if (!run.hr) return null;
        for (let i = 0; i < n - 1; i++) {
            out.push((run.hr[i] + run.hr[i + 1]) / 2);
        }
    } else {
        return null;
    }
    return out;
}

// Pro Lauf die fertigen Segment-Farben für die Ebene (global normiert).
function prepareRuns(runs, layer) {
    if (layer === 'heat') {
        return runs.map(r => ({ pts: r.pts, colors: null }));
    }

    const collected = [];
    const perRun = runs.map(r => {
        const values = segmentValues(r, layer);
        if (values) {
            for (const v of values) {
                if (v != null && isFinite(v)) collected.push(v);
            }
        }
        return { pts: r.pts, values };
    });

    let lo = 0, hi = 1, dirScale = 1;
    if (collected.length) {
        if (layer === 'direction') {
            const abs = collected.map(Math.abs).sort((a, b) => a - b);
            dirScale = abs[Math.floor(abs.length * 0.95)] || 1;
        } else {
            [lo, hi] = robustRange(collected);
        }
    }

    return perRun.map(({ pts, values }) => {
        if (!values) return { pts, colors: null };
        const colors = values.map(v => {
            if (v == null || !isFinite(v)) return 'rgba(205,212,224,0.55)';
            if (layer === 'direction') return directionColor(v, dirScale);
            const x = clamp((v - lo) / (hi - lo || 1), 0, 1);
            return layer === 'heartrate' ? hrColor(x) : warmColor(x);
        });
        return { pts, colors };
    });
}

// ---- Canvas-Overlay -----------------------------------------------------

function defineLayer() {
    if (HeatCanvasLayer) return;

    HeatCanvasLayer = L.Layer.extend({
        initialize(runs, layer) {
            this._runs = runs;
            this._layer = layer;
            this._prepared = prepareRuns(runs, layer);
        },
        onAdd(map) {
            this._map = map;
            // Bei aktiver Zoom-Animation als „leaflet-zoom-animated" markieren und auf zoomanim
            // reagieren, damit das Canvas WÄHREND des Zoomens mitskaliert. Sonst „schwebt" die
            // Strecke (Canvas-Overlay) auf Touch-Geräten und springt erst beim Loslassen zurück.
            const animated = map.options.zoomAnimation && L.Browser.any3d;
            const canvas = L.DomUtil.create('canvas', 'heat-canvas leaflet-layer leaflet-zoom-' + (animated ? 'animated' : 'hide'));
            canvas.style.position = 'absolute';
            canvas.style.pointerEvents = 'none';
            this._canvas = canvas;
            map.getPanes().overlayPane.appendChild(canvas);
            map.on('moveend resize', this._reset, this);
            if (animated) {
                map.on('zoomanim', this._animateZoom, this);
            }
            this._reset();
            return this;
        },
        onRemove(map) {
            map.off('moveend resize', this._reset, this);
            map.off('zoomanim', this._animateZoom, this);
            L.DomUtil.remove(this._canvas);
        },
        _animateZoom(e) {
            // Canvas per CSS-Transform mit der Zoom-Animation mitziehen (Muster wie Leaflet.heat
            // / Leaflets eigener Canvas-Renderer). _reset (auf moveend) zeichnet danach scharf neu.
            const scale = this._map.getZoomScale(e.zoom);
            const offset = this._map._getCenterOffset(e.center)._multiplyBy(-scale).subtract(this._map._getMapPanePos());
            L.DomUtil.setTransform(this._canvas, offset, scale);
        },
        _reset() {
            const size = this._map.getSize();
            this._canvas.width = size.x;
            this._canvas.height = size.y;
            L.DomUtil.setPosition(this._canvas, this._map.containerPointToLayerPoint([0, 0]));
            this._draw();
        },
        _draw() {
            const ctx = this._canvas.getContext('2d');
            ctx.clearRect(0, 0, this._canvas.width, this._canvas.height);
            const map = this._map;
            const at = ll => map.latLngToContainerPoint(ll);
            ctx.lineJoin = 'round';
            ctx.lineCap = 'round';

            if (this._layer === 'heat') {
                ctx.globalCompositeOperation = 'lighter';
                ctx.lineWidth = 3;
                ctx.strokeStyle = 'rgba(255,150,40,0.38)';
                for (const run of this._prepared) {
                    strokePath(ctx, run.pts, at);
                }
                ctx.globalCompositeOperation = 'source-over';
                return;
            }

            ctx.globalCompositeOperation = 'source-over';
            ctx.lineWidth = 4;
            for (const run of this._prepared) {
                const pts = run.pts;
                if (!run.colors) {
                    ctx.strokeStyle = 'rgba(205,212,224,0.55)'; // kein passender Stream
                    strokePath(ctx, pts, at);
                    continue;
                }
                for (let i = 0; i < pts.length - 1; i++) {
                    const a = at(pts[i]), b = at(pts[i + 1]);
                    ctx.strokeStyle = run.colors[i];
                    ctx.beginPath();
                    ctx.moveTo(a.x, a.y);
                    ctx.lineTo(b.x, b.y);
                    ctx.stroke();
                }
            }
        }
    });
}

function strokePath(ctx, pts, at) {
    if (pts.length < 2) return;
    ctx.beginPath();
    const p0 = at(pts[0]);
    ctx.moveTo(p0.x, p0.y);
    for (let i = 1; i < pts.length; i++) {
        const p = at(pts[i]);
        ctx.lineTo(p.x, p.y);
    }
    ctx.stroke();
}

// ---- Tap auf einen Lauf -------------------------------------------------
// Das Canvas-Overlay hat pointerEvents:none, daher kommt der Klick auf der Karten-Ebene an.
// Treffer-Test gegen die (projizierten) Lauf-Segmente mit fingerfreundlicher Pixel-Schwelle.

const TAP_THRESHOLD_PX = 15;

function distToSegment(p, a, b) {
    const dx = b.x - a.x, dy = b.y - a.y;
    const len2 = dx * dx + dy * dy;
    let t = len2 > 0 ? ((p.x - a.x) * dx + (p.y - a.y) * dy) / len2 : 0;
    t = clamp(t, 0, 1);
    return Math.hypot(p.x - (a.x + t * dx), p.y - (a.y + t * dy));
}

function escapeHtml(value) {
    return String(value ?? '').replace(/[&<>"']/g, c =>
        ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}

function runsNearPoint(map, point, runs) {
    const hits = [];
    for (const run of runs) {
        const pts = run.pts;
        let hit = false;
        for (let i = 0; i < pts.length - 1 && !hit; i++) {
            const a = map.latLngToContainerPoint(pts[i]);
            const b = map.latLngToContainerPoint(pts[i + 1]);
            if (distToSegment(point, a, b) <= TAP_THRESHOLD_PX) hit = true;
        }
        if (hit) hits.push(run);
    }
    return hits; // Reihenfolge wie geliefert (neueste zuerst)
}

function runPopupHtml(run, extraCount) {
    const info = run.info || {};
    const hr = info.heartRate ? ` · ${escapeHtml(info.heartRate)}` : '';
    const more = extraCount > 0
        ? `<span class="run-popup-more">+${extraCount} weitere(r) Lauf hier</span>` : '';
    return `<div class="run-popup">`
        + `<strong>${escapeHtml(info.name || 'Lauf')}</strong>`
        + `<span class="run-popup-date">${escapeHtml(info.date || '')}</span>`
        + `<span>${escapeHtml(info.distance || '')} · ${escapeHtml(info.pace || '')}${hr}</span>`
        + `${more}</div>`;
}

function showRunPopup(map, e, runs) {
    const hits = runsNearPoint(map, e.containerPoint, runs);
    if (!hits.length) return;
    // Empfehlung der Roadmap: jüngster Treffer (= erster, da neueste zuerst geliefert) + „+n weitere".
    L.popup({ className: 'run-popup-wrap', closeButton: true })
        .setLatLng(e.latlng)
        .setContent(runPopupHtml(hits[0], hits.length - 1))
        .openOn(map);
}

// ---- Öffentliche API ----------------------------------------------------

export async function render(elementId, runs, layer, focus) {
    await ensureLeaflet();
    defineLayer();

    const element = document.getElementById(elementId);
    if (!element) return;

    // Erst bauen, wenn der Container Maße hat – sonst schwarze Karte beim ersten Render.
    await waitForSize(element);

    if (element._heat?.map) {
        element._heat.ro?.disconnect();
        element._heat.map.remove();
    }

    const map = L.map(element, { preferCanvas: true }).setView([53.55, 9.99], 11); // Default: Hamburg
    // Lokaler Kachel-Proxy (siehe /tiles-Endpoint): das offline iPad bekommt die Karte vom
    // LAN-Server, der sie online lädt + cached. Keine externe CDN-Abhängigkeit mehr.
    const baseLayer = L.tileLayer('/tiles/{z}/{x}/{y}.png', {
        maxZoom: 19, // OSM-Kacheln gibt es bis Zoom 19
        attribution: '© OpenStreetMap'
    });

    // Beim ersten Schwung scheitern manche Kacheln (Anbieter drosselt) → gezielt und gestaffelt
    // erneut anfragen, damit sich die Lücken OHNE manuelles Neuladen von selbst füllen. Der
    // ?r=-Parameter umgeht den Negativ-Cache des Browsers; der Proxy ignoriert ihn.
    baseLayer.on('tileerror', e => {
        const img = e.tile;
        const attempt = (img._retry || 0) + 1;
        if (attempt > 4 || !e.coords) {
            return;
        }
        img._retry = attempt;
        setTimeout(() => {
            img.src = `/tiles/${e.coords.z}/${e.coords.x}/${e.coords.y}.png?r=${attempt}`;
        }, 700 * attempt);
    });

    baseLayer.addTo(map);

    const data = Array.isArray(runs) ? runs.filter(r => r && Array.isArray(r.pts) && r.pts.length > 1) : [];
    const overlay = new HeatCanvasLayer(data, layer || 'heat').addTo(map);
    element._heat = { map, overlay, runs: data };

    // Einmal binden – der Handler liest element._heat.runs, überlebt also Ebenen-Wechsel.
    map.on('click', e => showRunPopup(map, e, element._heat.runs));

    const bounds = L.latLngBounds([]);
    for (const run of data) {
        for (const point of run.pts) bounds.extend(point);
    }

    // Bei In-App-Navigation hat der Container beim Init evtl. noch nicht die endgültige Größe –
    // Leaflet zeigt dann eine leere/schwarze Karte mit nicht geladenen Kacheln. Nach dem Layout
    // neu vermessen (invalidateSize) und Ausschnitt setzen; mehrfach, um Timing-Fenster abzudecken.
    // Mit focus (Gesamt-Ansicht) auf den Heimat-Standort zentrieren statt über alle Läufe zu passen.
    const fit = () => {
        map.invalidateSize();
        if (focus) {
            map.setView([focus.lat, focus.lng], focus.zoom);
        } else if (bounds.isValid()) {
            map.fitBounds(bounds, { padding: [24, 24] });
        }
    };
    fit();
    requestAnimationFrame(fit);

    // Container-Größe kann sich nach dem Init noch ändern (spät angewandtes CSS, Layout nach
    // Navigation) – dann die Karte zuverlässig neu vermessen + einpassen.
    if (window.ResizeObserver) {
        const ro = new ResizeObserver(() => fit());
        ro.observe(element);
        element._heat.ro = ro;
    }
}

export function setLayer(elementId, layer) {
    const element = document.getElementById(elementId);
    const heat = element?._heat;
    if (!heat?.map) return;

    heat.overlay.remove();
    heat.overlay = new HeatCanvasLayer(heat.runs, layer || 'heat').addTo(heat.map);
}

export function dispose(elementId) {
    const element = document.getElementById(elementId);
    if (element && element._heat?.map) {
        element._heat.ro?.disconnect();
        element._heat.map.remove();
        element._heat = null;
    }
}
