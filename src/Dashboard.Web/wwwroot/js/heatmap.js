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
    if (document.querySelector(`link[href="${href}"]`)) {
        return;
    }
    const link = document.createElement('link');
    link.rel = 'stylesheet';
    link.href = href;
    document.head.appendChild(link);
}

function ensureLeaflet() {
    if (window.L) {
        return Promise.resolve();
    }
    if (leafletLoader) {
        return leafletLoader;
    }
    addStylesheet('https://unpkg.com/leaflet@1.9.4/dist/leaflet.css');
    leafletLoader = loadScript('https://unpkg.com/leaflet@1.9.4/dist/leaflet.js');
    return leafletLoader;
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
const warmColor = x => ramp(x, [51, 13, 5], [255, 90, 31], [255, 223, 128]);     // dunkel → orange → hell
const hrColor = x => ramp(x, [44, 127, 184], [255, 207, 51], [226, 59, 59]);     // blau → gelb → rot

function directionColor(signedGradient, scale) {
    const t = clamp(signedGradient / (scale || 1), -1, 1);
    const grey = [120, 120, 120];
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
            if (v == null || !isFinite(v)) return 'rgba(150,150,150,0.25)';
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
            const canvas = L.DomUtil.create('canvas', 'heat-canvas leaflet-zoom-hide');
            canvas.style.position = 'absolute';
            canvas.style.pointerEvents = 'none';
            this._canvas = canvas;
            map.getPanes().overlayPane.appendChild(canvas);
            map.on('moveend zoomend resize', this._reset, this);
            this._reset();
            return this;
        },
        onRemove(map) {
            map.off('moveend zoomend resize', this._reset, this);
            L.DomUtil.remove(this._canvas);
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
                ctx.lineWidth = 2;
                ctx.strokeStyle = 'rgba(255,90,30,0.22)';
                for (const run of this._prepared) {
                    strokePath(ctx, run.pts, at);
                }
                ctx.globalCompositeOperation = 'source-over';
                return;
            }

            ctx.globalCompositeOperation = 'source-over';
            ctx.lineWidth = 3;
            for (const run of this._prepared) {
                const pts = run.pts;
                if (!run.colors) {
                    ctx.strokeStyle = 'rgba(150,150,150,0.25)'; // kein passender Stream
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

// ---- Öffentliche API ----------------------------------------------------

export async function render(elementId, runs, layer) {
    await ensureLeaflet();
    defineLayer();

    const element = document.getElementById(elementId);
    if (!element) return;

    if (element._heat?.map) {
        element._heat.map.remove();
    }

    const map = L.map(element, { preferCanvas: true }).setView([53.55, 9.99], 11); // Default: Hamburg
    L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_nolabels/{z}/{x}/{y}{r}.png', {
        maxZoom: 20,
        subdomains: 'abcd',
        attribution: '© OpenStreetMap, © CARTO'
    }).addTo(map);

    const data = Array.isArray(runs) ? runs.filter(r => r && Array.isArray(r.pts) && r.pts.length > 1) : [];
    const overlay = new HeatCanvasLayer(data, layer || 'heat').addTo(map);
    element._heat = { map, overlay, runs: data };

    const bounds = L.latLngBounds([]);
    for (const run of data) {
        for (const point of run.pts) bounds.extend(point);
    }
    if (bounds.isValid()) {
        map.fitBounds(bounds, { padding: [24, 24] });
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
        element._heat.map.remove();
        element._heat = null;
    }
}
