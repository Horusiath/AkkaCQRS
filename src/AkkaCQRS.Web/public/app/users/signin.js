(function (app) {
    'use strict';

    app.controller('SigninController', [
        '$scope', 'Auth', function ($scope, Auth) {
            $scope.credentials = {
                email: '',
                password: '',
                rememberMe: false
            };

            $scope.signin = function(credentials) {
                Auth.signin(credentials);
            };
        }
    ]);

})(window.app);