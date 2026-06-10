// Leaflet + Heatmap-Layer für die Lauf-Heatmap (FA-8.05). Leaflet wird per CDN
// nachgeladen; clientseitig gerendert, die Punkte kommen aus Blazor.

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
    if (window.L && window.L.heatLayer) {
        return Promise.resolve();
    }
    if (leafletLoader) {
        return leafletLoader;
    }
    addStylesheet('https://unpkg.com/leaflet@1.9.4/dist/leaflet.css');
    leafletLoader = loadScript('https://unpkg.com/leaflet@1.9.4/dist/leaflet.js')
        .then(() => loadScript('https://unpkg.com/leaflet.heat@0.2.0/dist/leaflet-heat.js'));
    return leafletLoader;
}

export async function render(elementId, points) {
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

    const map = L.map(element).setView([53.55, 9.99], 11); // Default: Hamburg
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '© OpenStreetMap'
    }).addTo(map);

    if (points && points.length > 0) {
        L.heatLayer(points, { radius: 6, blur: 8, maxZoom: 14 }).addTo(map);
        map.fitBounds(points);
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
