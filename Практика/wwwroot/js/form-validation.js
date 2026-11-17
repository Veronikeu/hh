/* =============================================================
   Проверка полей формы при вводе.
   Правила задаются атрибутами поля: required, maxlength, min, max,
   pattern. Тексты ошибок — атрибутами data-msg-required,
   data-msg-format, data-msg-range. Ошибка выводится красным текстом
   в элементе .field-error под полем и исчезает, как только значение
   исправлено. Подключается к формам с атрибутом data-validate.
   ============================================================= */

(function () {
    "use strict";

    var FIELDS = "input:not([type=hidden]):not([type=checkbox]), select, textarea";

    /** Текст ошибки для поля или пустая строка, если значение верное. */
    function findError(field) {
        var value = field.value.trim();

        // В числовое поле введено не число (например, «1e»): браузер отдаёт пустое значение
        if (field.validity.badInput)
            return "Введите число";

        if (value === "")
            return isRequired(field) ? (field.dataset.msgRequired || "Заполните поле") : "";

        if (field.maxLength > 0 && value.length > field.maxLength)
            return "Не больше " + field.maxLength + " символов";

        if (field.pattern && !new RegExp("^(?:" + field.pattern + ")$", "u").test(value))
            return field.dataset.msgFormat || "Неверный формат";

        if (field.type === "number")
            return numberError(field, Number(value));

        return "";
    }

    function numberError(field, number) {
        if (!Number.isInteger(number))
            return "Только целое число";

        var belowMin = field.min !== "" && number < Number(field.min);
        var aboveMax = field.max !== "" && number > Number(field.max);
        if (belowMin || aboveMax)
            return field.dataset.msgRange || ("От " + field.min + " до " + field.max);

        return "";
    }

    /**
     * Поле обязательно, если у него есть атрибут required, или если у него есть
     * data-required-in-group и в той же группе (строке образования) заполнено другое поле.
     */
    function isRequired(field) {
        if (field.required)
            return true;

        var group = field.hasAttribute("data-required-in-group") && field.closest("[data-group]");
        if (!group)
            return false;

        return Array.prototype.some.call(group.querySelectorAll(FIELDS), function (other) {
            return other !== field && other.value.trim() !== "";
        });
    }

    /** Показывает или убирает ошибку под полем. Возвращает true, если значение верное. */
    function check(field) {
        var error = findError(field);
        var box = field.closest(".field");
        var slot = box ? box.querySelector(".field-error") : null;

        field.classList.toggle("input-validation-error", error !== "");
        field.setAttribute("aria-invalid", error !== "" ? "true" : "false");
        if (slot)
            slot.textContent = error;

        return error === "";
    }

    /**
     * Поле проверяется, когда пользователь его покинул, а после этого — при каждом изменении:
     * ошибка не мешает, пока значение набирается, и сразу исчезает, когда исправлено.
     */
    function attach(form) {
        var fields = Array.prototype.slice.call(form.querySelectorAll(FIELDS));

        // Вместо всплывающих подсказок браузера — свои сообщения под полями
        form.noValidate = true;

        fields.forEach(function (field) {
            // Поле с ошибкой, найденной сервером, проверяется заново при первом же изменении
            if (field.classList.contains("input-validation-error"))
                field.dataset.touched = "true";

            field.addEventListener("blur", function () {
                field.dataset.touched = "true";
                check(field);
            });

            // Изменение одного поля строки образования может сделать обязательным соседнее
            field.addEventListener("input", function () {
                var group = field.closest("[data-group]");
                var related = group ? group.querySelectorAll(FIELDS) : [field];

                Array.prototype.forEach.call(related, function (item) {
                    if (item.dataset.touched)
                        check(item);
                });
            });
        });

        form.addEventListener("submit", function (event) {
            var firstInvalid = null;

            fields.forEach(function (field) {
                field.dataset.touched = "true";
                if (!check(field) && !firstInvalid)
                    firstInvalid = field;
            });

            if (firstInvalid) {
                event.preventDefault();
                firstInvalid.focus();
            }
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        document.querySelectorAll("form[data-validate]").forEach(attach);
    });
})();
