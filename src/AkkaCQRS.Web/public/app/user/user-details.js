(function (app) {
    'use strict';

    app.controller('UserDetailsController', [
        '$scope', '$http', '$routeParams', '$location', function ($scope, $http, $routeParams, $location) {
            var userId = $routeParams['id'];
            if (userId) {
                $http.get('api/users/' + userId).then(function (response) {
                    $scope.user = response.data;
                });
            } else {
                $location.path('/signin');
            }

            $scope.createAccount = function () {
                $http.post('api/accounts', { ownerId: $scope.user.id })
                    .then(function (response) {
                        $scope.user.accountsIds.push(response.data.id);
                    });
            };
        }
    ]);

})(window.app);