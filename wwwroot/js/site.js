(function () {
    "use strict";

    function initializeNavigation() {
        const sidebar = document.getElementById("sidebar");
        const backdrop = document.getElementById("backdrop");
        const menuButton = document.querySelector(".menu-toggle");

        if (!sidebar || !backdrop || !menuButton) return;

        const mobileLayout = window.matchMedia("(max-width: 980px)");

        function isMobileMenuOpen() {
            return sidebar.classList.contains("open");
        }

        function setBackdrop(open) {
            backdrop.hidden = !open;
            document.body.classList.toggle("overlay-open", open);
        }

        function updateAccessibleState() {
            const expanded = mobileLayout.matches
                ? isMobileMenuOpen()
                : !document.body.classList.contains("sidebar-collapsed");

            menuButton.setAttribute("aria-expanded", String(expanded));
            menuButton.setAttribute(
                "aria-label",
                expanded ? "Recolher navegação" : "Abrir navegação");

            sidebar.toggleAttribute(
                "inert",
                mobileLayout.matches && !isMobileMenuOpen());
        }

        function closeMobileMenu() {
            sidebar.classList.remove("open");
            setBackdrop(false);
            updateAccessibleState();
        }

        menuButton.addEventListener("click", function () {
            if (mobileLayout.matches) {
                if (isMobileMenuOpen()) {
                    closeMobileMenu();
                } else {
                    sidebar.classList.add("open");
                    setBackdrop(true);
                    updateAccessibleState();
                }
                return;
            }

            document.body.classList.toggle("sidebar-collapsed");
            updateAccessibleState();
        });

        backdrop.addEventListener("click", closeMobileMenu);

        sidebar.addEventListener("click", function (event) {
            if (event.target.closest(".nav-link") && mobileLayout.matches) {
                closeMobileMenu();
            }
        });

        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape" && isMobileMenuOpen()) {
                closeMobileMenu();
                menuButton.focus();
            }
        });

        mobileLayout.addEventListener("change", function (event) {
            if (!event.matches) {
                sidebar.classList.remove("open");
                setBackdrop(false);
            } else {
                document.body.classList.remove("sidebar-collapsed");
            }
            updateAccessibleState();
        });

        updateAccessibleState();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initializeNavigation, { once: true });
    } else {
        initializeNavigation();
    }
})();
