window.secureStorage = (function () {
    const keyName = 'taskflow_key';

    async function getKey() {
        let raw = localStorage.getItem(keyName);
        if (!raw) {
            const key = await crypto.subtle.generateKey({ name: 'AES-GCM', length: 256 }, true, ['encrypt', 'decrypt']);
            const exported = await crypto.subtle.exportKey('raw', key);
            const b64 = btoa(String.fromCharCode(...new Uint8Array(exported)));
            localStorage.setItem(keyName, b64);
            return key;
        }
        const bytes = Uint8Array.from(atob(raw), c => c.charCodeAt(0));
        return await crypto.subtle.importKey('raw', bytes, { name: 'AES-GCM' }, false, ['encrypt', 'decrypt']);
    }

    function bufToB64(buf) {
        return btoa(String.fromCharCode(...new Uint8Array(buf)));
    }

    function b64ToBuf(b64) {
        return Uint8Array.from(atob(b64), c => c.charCodeAt(0));
    }

    return {
        async setEncryptedItem(k, value) {
            try {
                const key = await getKey();
                const enc = new TextEncoder().encode(value);
                const iv = crypto.getRandomValues(new Uint8Array(12));
                const ct = await crypto.subtle.encrypt({ name: 'AES-GCM', iv: iv }, key, enc);
                localStorage.setItem(k, JSON.stringify({ iv: bufToB64(iv), data: bufToB64(ct) }));
                return true;
            } catch (e) {
                return false;
            }
        },
        async getEncryptedItem(k) {
            try {
                const raw = localStorage.getItem(k);
                if (!raw) return null;
                const obj = JSON.parse(raw);
                const key = await getKey();
                const iv = b64ToBuf(obj.iv);
                const data = b64ToBuf(obj.data);
                const pt = await crypto.subtle.decrypt({ name: 'AES-GCM', iv: iv }, key, data);
                return new TextDecoder().decode(pt);
            } catch (e) {
                return null;
            }
        },
        removeItem(k) {
            localStorage.removeItem(k);
            return true;
        }
    };
})();
