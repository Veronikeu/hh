/* =============================================================
   Всплывающие уведомления (toast).
   Окно появляется в правом верхнем углу и само исчезает.
   ============================================================= */

(function () {
    "use strict";

    var ICONS = {
        success: "✔",  // галочка
        info: "ℹ",     // буква i
        warning: "⚠",  // восклицательный знак
        error: "✖"     // крестик
    };

    var LIFETIME = 6000;   // сколько миллисекунд окно остаётся на экране
    var POLL_PERIOD = 15000; // как часто проверять новые уведомления в базе

    function getArea() {
        var area = document.querySelector(".toast-area");
        if (!area) {
            area = document.createElement("div");
            area.className = "toast-area";
            document.body.appendChild(area);
        }
        return area;
    }

    function close(toast) {
        if (!toast || toast.classList.contains("toast--hide")) {
            return;
        }
        toast.classList.add("toast--hide");
        setTimeout(function () {
            if (toast.parentNode) {
                toast.parentNode.removeChild(toast);
            }
        }, 250);
    }

    /**
     * Показывает всплывающее уведомление.
     * @param {string} type  success | info | warning | error
     * @param {string} title заголовок
     * @param {string} text  текст сообщения
     */
    function showToast(type, title, text) {
        var kind = ICONS[type] ? type : "info";

        var toast = document.createElement("div");
        toast.className = "toast toast--" + kind;

        var icon = document.createElement("div");
        icon.className = "toast__icon";
        icon.textContent = ICONS[kind];

        var body = document.createElement("div");
        body.className = "toast__body";

        var titleEl = document.createElement("div");
        titleEl.className = "toast__title";
        titleEl.textContent = title || "";
        body.appendChild(titleEl);

        if (text) {
            var textEl = document.createElement("div");
            textEl.className = "toast__text";
            textEl.textContent = text;
            body.appendChild(textEl);
        }

        var closeBtn = document.createElement("button");
        closeBtn.className = "toast__close";
        closeBtn.type = "button";
        closeBtn.setAttribute("aria-label", "Закрыть уведомление");
        closeBtn.textContent = "×";
        closeBtn.addEventListener("click", function () { close(toast); });

        toast.appendChild(icon);
        toast.appendChild(body);
        toast.appendChild(closeBtn);
        getArea().appendChild(toast);

        setTimeout(function () { close(toast); }, LIFETIME);
    }

    /** Запрашивает у сервера новые уведомления и показывает их. */
    function pollNotifications() {
        fetch("/api/notifications/unread", { headers: { "Accept": "application/json" } })
            .then(function (response) {
                return response.ok ? response.json() : [];
            })
            .then(function (items) {
                if (!Array.isArray(items)) {
                    return;
                }
                items.forEach(function (item) {
                    showToast(item.type, item.title, item.message);
                });
            })
            .catch(function () {
                /* сеть недоступна — уведомления придут при следующей проверке */
            });
    }

    window.showToast = showToast;

    document.addEventListener("DOMContentLoaded", function () {
        // Сообщения о результате последнего действия передаёт сервер
        var source = document.getElementById("server-toasts");
        if (source && source.textContent.trim()) {
            try {
                JSON.parse(source.textContent).forEach(function (item, index) {
                    setTimeout(function () {
                        showToast(item.type, item.title, item.text);
                    }, index * 250);
                });
            } catch (e) {
                /* некорректные данные просто пропускаем */
            }
        }

        // Уведомления о событиях, созданных другими пользователями
        if (document.body.dataset.authenticated === "true") {
            pollNotifications();
            setInterval(pollNotifications, POLL_PERIOD);
        }
    });
})();
