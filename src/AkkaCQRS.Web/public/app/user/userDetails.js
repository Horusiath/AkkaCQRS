(function (app) {
    'use strict';

    app.controller('UserDetailsController', [
        '$scope', '$http', '$routeParams', '$location', function ($scope, $http, $routeParams, $location) {
            var userId = $routeParams['id'];
            if (userId) {
                $http.get('api/users/' + userId).then(function(response) {
                    $scope.user = response.data;
                });
            } else {
                $location.path('/signin');
            }
        }
    ]);

})(window.app);