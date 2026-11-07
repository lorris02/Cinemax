// Live movie search: shows catalogue and TMDB matches while the visitor types.
(function () {
    const MIN_LENGTH = 2;
    const WAIT_MS = 180;

    const element = (tag, className, text) => {
        const node = document.createElement(tag);
        if (className) node.className = className;
        if (text) node.textContent = text;
        return node;
    };

    const openTmdb = (id) => {
        const form = document.getElementById('open-tmdb');
        form.elements.tmdbId.value = id;
        form.submit();
    };

    const fetchMatches = async (query, signal) => {
        const response = await fetch(`/Search/Suggest?q=${encodeURIComponent(query)}`, { signal });
        return response.ok ? response.json() : { local: [], tmdb: [] };
    };

    const resultRow = (movie, note) => {
        const row = element(movie.url ? 'a' : 'button', 'search-row');
        row.setAttribute('role', 'option');
        if (movie.url) {
            row.href = movie.url;
        } else {
            row.type = 'button';
            row.addEventListener('click', () => openTmdb(movie.tmdbId));
        }
        if (movie.poster || movie.posterUrl) {
            const image = element('img');
            image.src = movie.poster || movie.posterUrl;
            image.alt = '';
            image.loading = 'lazy';
            row.append(image);
        } else {
            row.append(element('span', 'search-row-empty'));
        }
        const text = element('span', 'search-row-text');
        text.append(element('strong', '', movie.title), element('small', '', [movie.year, note].filter(Boolean).join(' · ')));
        row.append(text);
        return row;
    };

    const attachLiveSearch = (input) => {
        const panel = element('div', 'search-panel');
        panel.id = `search-results-${Math.random().toString(36).slice(2)}`;
        panel.setAttribute('role', 'listbox');
        panel.hidden = true;
        input.closest('form').append(panel);
        input.setAttribute('role', 'combobox');
        input.setAttribute('aria-controls', panel.id);
        input.setAttribute('aria-expanded', 'false');

        let timer;
        let request;
        let active = -1;
        const rows = () => [...panel.querySelectorAll('.search-row')];
        const show = (visible) => {
            panel.hidden = !visible;
            input.setAttribute('aria-expanded', String(visible));
            if (!visible) active = -1;
        };
        const highlight = (index) => {
            const all = rows();
            active = (index + all.length) % all.length;
            all.forEach((row, position) => row.classList.toggle('active', position === active));
            all[active].scrollIntoView({ block: 'nearest' });
        };
        const render = ({ local, tmdb }, query) => {
            panel.replaceChildren();
            active = -1;
            if (local.length) {
                panel.append(element('p', 'search-label', 'In Cinemax'), ...local.map((movie) => resultRow(movie)));
            }
            if (tmdb.length) {
                panel.append(element('p', 'search-label', 'More from TMDB'), ...tmdb.map((movie) => resultRow(movie, 'Open to add')));
            }
            if (!local.length && !tmdb.length) {
                panel.append(element('p', 'search-none', `No movies found for "${query}".`));
            }
            show(true);
        };
        const search = async () => {
            const query = input.value.trim();
            request?.abort();
            if (query.length < MIN_LENGTH) {
                show(false);
                return;
            }
            request = new AbortController();
            panel.classList.add('loading');
            try {
                render(await fetchMatches(query, request.signal), query);
                panel.classList.remove('loading');
            } catch (error) {
                if (error.name !== 'AbortError') {
                    panel.classList.remove('loading');
                    show(false);
                }
            }
        };

        input.addEventListener('input', () => {
            clearTimeout(timer);
            timer = setTimeout(search, WAIT_MS);
        });
        input.addEventListener('focus', () => {
            if (panel.childElementCount && input.value.trim().length >= MIN_LENGTH) show(true);
        });
        input.addEventListener('keydown', (event) => {
            if (panel.hidden) return;
            if (event.key === 'Escape') {
                show(false);
            } else if ((event.key === 'ArrowDown' || event.key === 'ArrowUp') && rows().length) {
                event.preventDefault();
                highlight(active + (event.key === 'ArrowDown' ? 1 : -1));
            } else if (event.key === 'Enter' && active >= 0) {
                event.preventDefault();
                rows()[active].click();
            }
        });
        document.addEventListener('click', (event) => {
            if (!input.closest('form').contains(event.target)) show(false);
        });
    };

    // On the results page, TMDB fills in movies the catalogue doesn't have yet.
    const loadTmdbResults = async (section) => {
        const empty = document.getElementById('no-local');
        try {
            const { tmdb } = await fetchMatches(section.dataset.query);
            if (tmdb.length) {
                section.querySelector('.movie-grid').append(...tmdb.map((movie) => {
                    const card = element('button', 'movie-card movie-card-button');
                    card.type = 'button';
                    const poster = element('span', 'poster');
                    if (movie.posterUrl) {
                        const image = element('img');
                        image.src = movie.posterUrl;
                        image.alt = `Poster for ${movie.title}`;
                        image.loading = 'lazy';
                        poster.append(image);
                    } else {
                        poster.append(element('span', 'poster-empty', movie.title));
                    }
                    card.append(poster, element('span', 'movie-card-title', movie.title), element('span', 'movie-card-meta', movie.year || ''));
                    card.addEventListener('click', () => openTmdb(movie.tmdbId));
                    return card;
                }));
                section.hidden = false;
                empty?.remove();
            } else if (empty) {
                empty.textContent = 'No movies found. Try a different spelling or a shorter search.';
            }
        } catch {
            if (empty) empty.textContent = 'No movies found in Cinemax.';
        }
    };

    document.querySelectorAll('[data-live-search]').forEach(attachLiveSearch);
    const tmdbSection = document.getElementById('tmdb-results');
    if (tmdbSection) loadTmdbResults(tmdbSection);
})();
