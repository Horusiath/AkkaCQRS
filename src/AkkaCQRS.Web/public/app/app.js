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
                    controller: 'SigninController',
                    templateUrl: 'app/auth/signin.html'
                })
                .when('/user/:id', {
                    controller: 'UserDetailsController',
                    templateUrl: 'app/user/userDetails.html'
                });
        }]);

    window.app = app;
})();