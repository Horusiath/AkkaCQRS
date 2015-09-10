var __decorate = this.__decorate || function (decorators, target, key, desc) {
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") return Reflect.decorate(decorators, target, key, desc);
    switch (arguments.length) {
        case 2: return decorators.reduceRight(function(o, d) { return (d && d(o)) || o; }, target);
        case 3: return decorators.reduceRight(function(o, d) { return (d && d(target, key)), void 0; }, void 0);
        case 4: return decorators.reduceRight(function(o, d) { return (d && d(target, key, o)) || o; }, desc);
    }
};
define(["require", "exports", "aurelia-framework"], function (require, exports, aurelia_framework_1) {
    var SignIn = (function () {
        function SignIn() {
            this.username = 'John Snow';
            this.password = '';
            this.remember = false;
        }
        __decorate([
            aurelia_framework_1.bindable
        ], SignIn.prototype, "username");
        __decorate([
            aurelia_framework_1.bindable
        ], SignIn.prototype, "password");
        __decorate([
            aurelia_framework_1.bindable
        ], SignIn.prototype, "remember");
        return SignIn;
    })();
    exports.SignIn = SignIn;
});
//# sourceMappingURL=signin.js.map