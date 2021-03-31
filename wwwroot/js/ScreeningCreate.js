//onload =>get intervale disponibile
var date = $('#date').val();


var hallid = $('#hallId').val();

$(document).ready(function () {
    $.get("/Screening/GetHours?date=" + date + "&hallid=" + hallid,
        function (data) {
            $("#intervals").html(data);

        });
});
//daca se schimba hallid sau daca se schimba date =>get intervale disponibile
$("#hallId").change(function () {

    var date = $('#date').val();
    var hallid = $('#hallId').val();

    $.get("/Screening/GetHours?date=" + date + "&hallid=" + hallid,
        function (data) {
            $("#intervals").html(data);

        });

});


$("#date").change(function () {

    var date = $('#date').val();
    var hallid = $('#hallId').val();

    $.get("/Screening/GetHours?date=" + date + "&hallid=" + hallid,
        function (data) {

            $("#intervals").html(data);

        });

});