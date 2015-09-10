define(["require", "exports"], function (require, exports) {
    function configure(aurelia) {
        if (!!aurelia) {
            aurelia.use
                .standardConfiguration()
                .developmentLogging();
            aurelia.start().then(function (a) { return a.setRoot('app/app', document.body); });
        }
    }
    exports.configure = configure;
});
//# sourceMappingURL=main.js.map