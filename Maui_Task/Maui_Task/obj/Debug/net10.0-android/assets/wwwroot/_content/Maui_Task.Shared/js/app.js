/*
  UI REPLICATION [AppUtilities]:
  SOURCE: Task_Flow frontend utility behavior (outside-click, focus, scroll helpers)
  TARGET: Maui_Task.Shared/wwwroot/js/app.js
  REPLICATED:
    - Utility helpers for click-outside, focus, and scroll interactions
    - Blazor translation note: functions exposed on window for JS interop calls
*/

window.taskFlowApp = {
    onClickOutside: function (dotNetRef, elementId) {
        const handler = function (e) {
            const el = document.getElementById(elementId);
            if (el && !el.contains(e.target)) {
                dotNetRef.invokeMethodAsync('CloseFromOutside');
                document.removeEventListener('click', handler);
            }
        };

        setTimeout(function () {
            document.addEventListener('click', handler);
        }, 100);
    },

    removeClickOutside: function () {
        // Listener auto-removes after first trigger.
    },

    focusElement: function (elementId) {
        const el = document.getElementById(elementId);
        if (el) {
            el.focus();
        }
    },

    scrollIntoView: function (elementId) {
        const el = document.getElementById(elementId);
        if (el) {
            el.scrollIntoView({ behavior: 'smooth' });
        }
    },

    scrollToTop: function (elementId) {
        const el = document.getElementById(elementId);
        if (el) {
            el.scrollTop = 0;
            return;
        }
        window.scrollTo({ top: 0, behavior: 'smooth' });
    },

    isNearBottom: function (elementId, threshold) {
        const el = document.getElementById(elementId);
        if (!el) {
            return false;
        }
        return el.scrollHeight - el.scrollTop - el.clientHeight < (threshold || 100);
    }
};