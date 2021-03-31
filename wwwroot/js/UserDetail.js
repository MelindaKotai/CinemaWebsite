$(document).ready(function () {
    $('#claimedp').click(function (e) {

        $(e.target).addClass('active');
        $('#unclaimedp').removeClass('active');
        $('#claimed').removeClass('hide');
        $('#unclaimed').addClass('hide');

    });
    $('#unclaimedp').click(function (e) {

        $(e.target).addClass('active');
        $('#claimedp').removeClass('active');
        $('#unclaimed').removeClass('hide');
        $('#claimed').addClass('hide');
    });


});
