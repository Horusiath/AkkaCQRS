(function (app) {
    'use strict';


    app.factory('Auth', ['$http',
        function ($http) {
            var auth = {
                user: null
            };

            auth.signin = function (credentials) {
                return $http.post('api/users/signin', credentials)
                    .then(function (response) {
                        return auth.user = response.data;
                    });
            };

            auth.signup = function (user) {
                return $http.post('api/users/', user)
                    .then(function (response) {
                        return auth.user = response.data;
                    });
            };

            auth.isAuthenticated = function () {
                return auth.user !== null;
            };

            return auth;
        }
    ]);

    app.controller('NavController', [
        '$scope', 'Auth', function($scope, Auth) {
            $scope.user = Auth.user;
            $scope.authenticated = Auth.isAuthenticated();
        }
    ]);

})(window.app);