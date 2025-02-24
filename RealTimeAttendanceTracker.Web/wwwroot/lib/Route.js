var app = angular.module('attendanceModule', []);

function ErrorEvt(str) {
    Swal.fire({
        title: "Technical Issue!!",
        text: "Please try again later! If issue persists, please contact sssd tech solutions pvt ltd.",
        icon: "error"
    });
}
function getHostedUrl() {
    return $("#webHosted").val();
}
function getToken() {
    return $('input[name="__RequestVerificationToken"]').val();
}
function SuccessEvt(str) {
    Swal.fire({
        title: "Success",
        text: "Request has been completed successfully",
        icon: "success"
    });
}
app.controller('ctrlAttendance', ['$scope', '$http', '$sce', "$window", function ($scope, $http, $sce, $window) {
    $scope.PageInputs = {};
    $scope.GridData = [];
    $scope.IsUpdate = false;
    $scope.EditStudent = function (key) {
        window.location.href = getHostedUrl() + "/Home/AddUpdateStudents?id=" + key;
    }
    $scope.EditStaff = function (key) {
        window.location.href = getHostedUrl() + "/Home/AddUpdateStaff?id=" + key;
    }
    $scope.DeleteStudent = function (items) {
        $http({
            async: false,
            cache: false,
            url: getHostedUrl() + "/Home/DeleteStudent?id=" + items.id,
            method: "POST"
        }).then(function (data, status, headers, config) {
            SuccessEvt();
            $scope.GetStudents();
        }, function errorCallback(response) {
            ErrorEvt();
        });
    }
    $scope.DeleteStaff = function (items) {
        $http({
            async: false,
            cache: false,
            url: getHostedUrl() + "/Home/DeleteStaff?id=" + items.id,
            method: "POST"
        }).then(function (data, status, headers, config) {
            SuccessEvt();
            $scope.GetStaffs();
        }, function errorCallback(response) {
            ErrorEvt();
        });
    }
    $scope.GetStudents = function () {
        $http({
            async: false,
            cache: false,
            url: getHostedUrl() + "/Home/GetStudents",
            method: "GET"
        }).then(function (data, status, headers, config) {
            $scope.GridData = [];
            $scope.GridData = data.data;
        }, function errorCallback(response) {
            ErrorEvt();
        });
    }
    $scope.GetStaffs = function () {
        $http({
            async: false,
            cache: false,
            url: getHostedUrl() + "/Home/GetStaffs",
            method: "GET"
        }).then(function (data, status, headers, config) {
            $scope.GridData = [];
            $scope.GridData = data.data;
        }, function errorCallback(response) {
            ErrorEvt();
        });
    }
}]);
