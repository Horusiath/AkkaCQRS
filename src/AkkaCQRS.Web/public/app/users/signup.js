(function () {
    'use strict';

    app.controller('SignupController', [
        '$scope', 'Auth', function ($scope, Auth) {
            $scope.user = {
                firstName: '',
                lastName: '',
                email: '',
                password: '',
                passwordRepeat: ''
            };
            $scope.register = function (user) {
                if (user.password === user.passwordRepeat) {
                    Auth.signup(user);
                }
            };
        }
    ]);

})();