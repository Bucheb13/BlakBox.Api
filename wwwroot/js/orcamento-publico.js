(() => {
    const download = document.querySelector('[data-download]');

    if (!download) {
        return;
    }

    download.addEventListener('click', () => {
        download.setAttribute('aria-busy', 'true');
        download.setAttribute(
            'aria-label',
            'Preparando o PDF do orçamento'
        );

        download.innerHTML =
            '<span aria-hidden="true">…</span> Preparando PDF';

        window.setTimeout(() => {
            download.removeAttribute('aria-busy');
            download.removeAttribute('aria-label');

            download.innerHTML =
                '<span aria-hidden="true">↓</span> Baixar orçamento em PDF';
        }, 4000);
    });
})();