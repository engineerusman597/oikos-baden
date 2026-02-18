export function setCookie(key, value) {
    document.cookie = key + "=" + encodeURIComponent(value) + "; path=/; samesite=strict; secure";
}

export function deleteCookie(key) {
    document.cookie = key + "=; path=/; max-age=0; expires=Thu, 01 Jan 1970 00:00:00 GMT; samesite=strict; secure";
}

export function getCookie(key) {
    var name = key + "=";
    var decodedCookie = decodeURIComponent(document.cookie);
    var ca = decodedCookie.split(';');
    for(var i = 0; i <ca.length; i++) {
        var c = ca[i];
        while (c.charAt(0) == ' ') {
            c = c.substring(1);
        }
        if (c.indexOf(name) === 0) {
            return c.substring(name.length, c.length);
        }
    }
    return "";
}