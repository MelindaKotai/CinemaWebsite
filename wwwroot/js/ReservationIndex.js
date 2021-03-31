$(document).ready(function () {
    $('#pastreservations').click(function (e) {

        $(e.target).addClass('active');
        $('#futurereservations').removeClass('active');
        $('#pasttable').removeClass('hide');
        $('#futuretable').addClass('hide');

    });
    $('#futurereservations').click(function (e) {

        $(e.target).addClass('active');
        $('#pastreservations').removeClass('active');
        $('#futuretable').removeClass('hide');
        $('#pasttable').addClass('hide');
    });


});