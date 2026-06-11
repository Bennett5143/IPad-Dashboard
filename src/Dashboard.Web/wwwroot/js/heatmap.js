// Lauf-Heatmap im Stil von Sam Wilsons "running-heatmap" / Stravas Personal Heatmap:
// jede Aktivität wird als zusammenhängende Linie auf dunkler Karte gezeichnet. Wo sich
// Strecken überlagern (oft gelaufen), addiert sich die Deckkraft -> die Route leuchtet heller.
// Leaflet wird per CDN nachgeladen; gerendert wird auf einem Canvas (additive Überlagerung,
// gute Performance bei vielen Linien). Die Tracks kommen pro Lauf gruppiert aus Blazor.

let leafletLoader = null;

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

// Zwei Linien pro Lauf: ein breiter, sehr schwacher "Glow" + ein schmaler, kräftiger Kern.
// Niedrige Deckkraft, damit sich überlagernde Strecken sichtbar aufsummieren.
const GLOW_STYLE = { color: '#ff5a1f', weight: 7, opacity: 0.06, lineCap: 'round', lineJoin: 'round', interactive: false };
const CORE_STYLE = { color: '#ff8a3d', weight: 2, opacity: 0.45, lineCap: 'round', lineJoin: 'round', interactive: false };

export async function render(elementId, tracks) {
    await ensureLeaflet();

    const element = document.getElementById(elementId);
    if (!element) {
        return;
    }

    // Vorherige Karte entfernen (z. B. nach Zeitraum-Wechsel).
    if (element._leafletMap) {
        element._leafletMap.remove();
        element._leafletMap = null;
    }

    const renderer = L.canvas({ padding: 0.5 });
    const map = L.map(element, { renderer, preferCanvas: true }).setView([53.55, 9.99], 11); // Default: Hamburg

    // Dunkle, label-arme Basiskarte (kostenlos, kein Key) – passt zum leuchtenden Routen-Look.
    L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_nolabels/{z}/{x}/{y}{r}.png', {
        maxZoom: 20,
        subdomains: 'abcd',
        attribution: '© OpenStreetMap, © CARTO'
    }).addTo(map);

    const routes = Array.isArray(tracks) ? tracks : [];
    const bounds = L.latLngBounds([]);

    for (const track of routes) {
        if (!Array.isArray(track) || track.length < 2) {
            continue;
        }
        L.polyline(track, { ...GLOW_STYLE, renderer }).addTo(map);
        L.polyline(track, { ...CORE_STYLE, renderer }).addTo(map);
        for (const point of track) {
            bounds.extend(point);
        }
    }

    if (bounds.isValid()) {
        map.fitBounds(bounds, { padding: [24, 24] });
    }

    element._leafletMap = map;
}

export function dispose(elementId) {
    const element = document.getElementById(elementId);
    if (element && element._leafletMap) {
        element._leafletMap.remove();
        element._leafletMap = null;
    }
}
