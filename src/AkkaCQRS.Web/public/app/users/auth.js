(function (app) {
    'use strict';


    app.factory('Auth', ['$http',
        function ($http) {
            var auth = {
                user: null
            };

            auth.signin = function (credentials) {
                $http.post('api/users/signin', credentials)
                    .then(function (response) {
                        auth.user = response;
                    });
            };

            auth.signup = function (user) {
                $http.post('api/users/', user)
                    .then(function (response) {
                        auth.user = response;
                    });
            };

            return auth;
        }
    ]);
})(window.app);