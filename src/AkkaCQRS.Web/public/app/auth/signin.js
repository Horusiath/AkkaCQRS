(function (app) {
    'use strict';

    app.controller('SigninController', [
        '$scope', '$location', 'Auth', function ($scope, $location, Auth) {
            $scope.credentials = {
                email: '',
                password: '',
                rememberMe: false
            };

            $scope.signin = function(credentials) {
                Auth.signin(credentials).then(function (signedAs) {
                    $location.path('/user/' + signedAs.userId);
                });;
            };
        }
    ]);

})(window.app);