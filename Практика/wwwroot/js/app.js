/* =============================================================
   Общие действия интерфейса.
   ============================================================= */

(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        // Подтверждение перед удалением записи
        document.querySelectorAll("form[data-confirm]").forEach(function (form) {
            form.addEventListener("submit", function (event) {
                if (!window.confirm(form.dataset.confirm)) {
                    event.preventDefault();
                }
            });
        });
    });
})();
