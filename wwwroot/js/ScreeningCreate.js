//onload =>get intervale disponibile
var date = $('#date').val();


var hallid = $('#hallId').val();

$(document).ready(function () {
    $.get("/Screening/GetHours?date=" + date + "&hallid=" + hallid,
        function (data) {
           
            var result = "";
            for (let i = 0; i < data.length; i++) {
                result = result+ data[i].Key + "-" + data[i].Value + "<br/>";
            }
            $("#intervals").html(result);
        });
});

//daca se schimba hallid sau daca se schimba date =>get intervale disponibile
$("#hallId").change(function () {
    var date = $('#date').val();
    var hallid = $('#hallId').val();
    $.get("/Screening/GetHours?date=" + date + "&hallid=" + hallid,
        function (data) {
            
            var result = "";
            for (let i = 0; i < data.length; i++) {
                result =result+ data[i].Key + "-" + data[i].Value + "<br/>";
            }
            $("#intervals").html(result);

        });
});

$("#date").change(function () {
    var date = $('#date').val();
    var hallid = $('#hallId').val();
    $.get("/Screening/GetHours?date=" + date + "&hallid=" + hallid,
        function (data) {
           
            var result = "";
            for (let i = 0; i < data.length; i++) {
                result = result+data[i].Key + "-" + data[i].Value + "<br/>";
            }
            $("#intervals").html(result);
        });
});