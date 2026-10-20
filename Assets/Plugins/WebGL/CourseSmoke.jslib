mergeInto(LibraryManager.library, {
    CourseSmokeReport: function (pointer) {
        var element = document.getElementById('course-test-result');
        if (!element) {
            element = document.createElement('pre');
            element.id = 'course-test-result';
            element.style.cssText = 'position:fixed;left:8px;bottom:8px;max-width:600px;max-height:140px;overflow:auto;color:#fff;background:#102030;z-index:9999;padding:8px;font-size:12px';
            document.body.appendChild(element);
        }
        element.textContent = UTF8ToString(pointer);
    }
});
