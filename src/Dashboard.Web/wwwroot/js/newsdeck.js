// Blätter-Ansicht der Recherche-Meldungen. Das Wischen macht der Browser (scroll-snap); hier
// wandern nur der Zähler und der Aktiv-Zustand der Pfeile mit — direkt im DOM, damit keine Geste
// über den Blazor-Circuit läuft.

const decks = new Map();

function panelWidth(track) {
    // Panels sind gleich breit; clientWidth ist die Breite genau eines Panels.
    return track.clientWidth || 1;
}

function currentIndex(track) {
    return Math.round(track.scrollLeft / panelWidth(track));
}

function update(deckId) {
    const deck = decks.get(deckId);
    if (!deck) {
        return;
    }

    const { track, counter } = deck;
    const total = track.children.length;
    const index = Math.min(Math.max(currentIndex(track), 0), Math.max(total - 1, 0));

    if (counter) {
        counter.textContent = `${index + 1} / ${total}`;
    }

    // An den Enden wird nicht umgebrochen: der Pfeil in die unmögliche Richtung geht aus.
    setDisabled(deck.prev, index <= 0);
    setDisabled(deck.next, index >= total - 1);
}

function setDisabled(button, disabled) {
    if (!button) {
        return;
    }

    button.classList.toggle('is-disabled', disabled);
    button.disabled = disabled;
}

export function attach(deckId, counterElementId) {
    const track = document.getElementById(deckId);
    if (!track) {
        return;
    }

    detach(deckId);

    const deck = {
        track,
        counter: counterElementId ? document.getElementById(counterElementId) : null,
        prev: document.getElementById(`${deckId}-prev`),
        next: document.getElementById(`${deckId}-next`),
        onScroll: null
    };

    // Während des Wischens feuert scroll dicht; ein Frame Verzögerung reicht völlig.
    let queued = false;
    deck.onScroll = () => {
        if (queued) {
            return;
        }

        queued = true;
        requestAnimationFrame(() => {
            queued = false;
            update(deckId);
        });
    };

    track.addEventListener('scroll', deck.onScroll, { passive: true });
    decks.set(deckId, deck);
    update(deckId);
}

export function page(deckId, direction) {
    const deck = decks.get(deckId);
    if (!deck) {
        return;
    }

    deck.track.scrollBy({ left: direction * panelWidth(deck.track), behavior: 'smooth' });
}

export function detach(deckId) {
    const deck = decks.get(deckId);
    if (!deck) {
        return;
    }

    deck.track.removeEventListener('scroll', deck.onScroll);
    decks.delete(deckId);
}
