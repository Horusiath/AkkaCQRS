/// <reference path="../typings/rx.d.ts" />
define(["require", "exports"], function (require, exports) {
    var samples;
    (function (samples) {
        var data;
        (function (data) {
            var Store = (function () {
                function Store(key) {
                    this.key = key;
                    this.updates = new Rx.BehaviorSubject(null);
                }
                return Store;
            })();
            data.Store = Store;
        })(data = samples.data || (samples.data = {}));
    })(samples = exports.samples || (exports.samples = {}));
});
//# sourceMappingURL=store.js.map