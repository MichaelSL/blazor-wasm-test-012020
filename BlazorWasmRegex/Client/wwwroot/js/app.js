// Earlier versions registered a PWA service worker that served a cached index.html for every
// navigation. That page no longer exists, so remove any worker still installed in a browser.
if ('serviceWorker' in navigator) {
    navigator.serviceWorker.getRegistrations().then(function (registrations) {
        for (const registration of registrations) {
            registration.unregister();
        }
    });
}

// Used by RegexTester's export buttons.
window.downloadFile = function (filename, content, contentType) {
    const blob = new Blob([content], { type: contentType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
};
