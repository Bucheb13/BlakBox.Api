/* ============================================================
   BLAKBOX — SITE
   JAVASCRIPT GLOBAL
   ============================================================ */

(function () {

    "use strict";


    /* ========================================================
       ELEMENTOS
       ======================================================== */

    const sidebar =
        document.getElementById("sidebar");

    const backdrop =
        document.getElementById("backdrop");

    const menuButton =
        document.querySelector(".menu-toggle");


    if (!sidebar || !backdrop || !menuButton) {
        return;
    }


    /* ========================================================
       BACKDROP
       ======================================================== */

    function setBackdrop(open) {

        backdrop.hidden = !open;

        backdrop.style.pointerEvents =
            open ? "auto" : "none";

        document.body.classList.toggle(
            "overlay-open",
            open
        );
    }


    /* ========================================================
       FECHAR MENU
       ======================================================== */

    function closeMenu() {

        sidebar.classList.remove("open");

        menuButton.setAttribute(
            "aria-expanded",
            "false"
        );

        setBackdrop(false);
    }


    /* ========================================================
       ABRIR MENU
       ======================================================== */

    function openMenu() {

        sidebar.classList.add("open");

        menuButton.setAttribute(
            "aria-expanded",
            "true"
        );

        setBackdrop(true);
    }


    /* ========================================================
       TOGGLE
       ======================================================== */

    menuButton.addEventListener(
        "click",
        function (event) {

            event.preventDefault();
            event.stopPropagation();

            const isOpen =
                sidebar.classList.contains("open");

            if (isOpen) {
                closeMenu();
            }
            else {
                openMenu();
            }
        }
    );


    /* ========================================================
       BACKDROP
       ======================================================== */

    backdrop.addEventListener(
        "click",
        function () {

            closeMenu();
        }
    );


    /* ========================================================
       FECHAR AO CLICAR NA NAVEGAÇÃO
       ======================================================== */

    sidebar.addEventListener(
        "click",
        function (event) {

            const link =
                event.target.closest(".nav-link");

            if (!link) {
                return;
            }

            closeMenu();
        }
    );


    /* ========================================================
       ESC
       ======================================================== */

    document.addEventListener(
        "keydown",
        function (event) {

            if (event.key === "Escape") {
                closeMenu();
            }
        }
    );


    /* ========================================================
       RIPPLE
       ======================================================== */

    document.addEventListener(
        "pointerdown",
        function (event) {

            const target =
                event.target.closest(
                    ".btn, .nav-link"
                );

            if (!target) {
                return;
            }

            const rect =
                target.getBoundingClientRect();

            const x =
                event.clientX - rect.left;

            const y =
                event.clientY - rect.top;

            const ripple =
                document.createElement("span");

            ripple.className =
                "ripple";

            ripple.style.left =
                x + "px";

            ripple.style.top =
                y + "px";

            target.appendChild(ripple);


            window.setTimeout(
                function () {

                    try {
                        ripple.remove();
                    }
                    catch (_) {
                    }

                },
                700
            );
        },
        {
            passive: true
        }
    );


    /* ========================================================
       CORRIGE ESTADO AO VOLTAR PARA DESKTOP
       ======================================================== */

    window.addEventListener(
        "resize",
        function () {

            if (window.innerWidth > 980) {

                closeMenu();
            }
        }
    );

})();