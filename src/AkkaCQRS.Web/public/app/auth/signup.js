(function () {
    'use strict';

    app.controller('SignupController', [
        '$scope', '$location', 'Auth', function ($scope, $location, Auth) {
            $scope.user = {
                firstName: 'John',
                lastName: 'Doe',
                email: 'j.doe@fakemail.co',
                password: 'pass',
                passwordRepeat: 'pass'
            };
            $scope.register = function (user) {
                if (user.password === user.passwordRepeat) {
                    Auth.signup(user).then(function(registered) {
                        $location.path('/user/' + registered.userId);
                    });
                }
            };
        }
    ]);

})();