/// <reference path="../../jspm_packages/github/aurelia/framework@0.13.4/aurelia-framework.d.ts" />
/// <reference path="../../jspm_packages/github/aurelia/templating@0.13.15/aurelia-templating.d.ts" />
/// <reference path="../../jspm_packages/github/aurelia/dependency-injection@0.9.1/aurelia-dependency-injection.d.ts" />
var __decorate = this.__decorate || function (decorators, target, key, desc) {
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") return Reflect.decorate(decorators, target, key, desc);
    switch (arguments.length) {
        case 2: return decorators.reduceRight(function(o, d) { return (d && d(o)) || o; }, target);
        case 3: return decorators.reduceRight(function(o, d) { return (d && d(target, key)), void 0; }, void 0);
        case 4: return decorators.reduceRight(function(o, d) { return (d && d(target, key, o)) || o; }, desc);
    }
};
define(["require", "exports", 'aurelia-framework'], function (require, exports, aurelia_framework_1) {
    var Auth = (function () {
        function Auth() {
            this.activeTab = 'login';
        }
        __decorate([
            aurelia_framework_1.bindable
        ], Auth.prototype, "activeTab");
        Auth = __decorate([
            aurelia_framework_1.autoinject
        ], Auth);
        return Auth;
    })();
    exports.Auth = Auth;
});
//# sourceMappingURL=auth.js.map