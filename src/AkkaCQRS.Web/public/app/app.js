(function () {
    'use strict';

    var app = angular.module('app', ['ngRoute'])
        .config(['$routeProvider', function ($routeProvider) {
            $routeProvider
                .when('/signup', {
                    controller: 'SignupController',
                    templateUrl: 'app/auth/signup.html'
                })
                .when('/signin', {
                    controller: 'SignInController',
                    templateUrl: 'app/auth/signin.html'
                });
        }]);

    window.app = app;
})();