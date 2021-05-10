$(".state").change(function () {
    var id = $(this).val();
    $.post("/Reservation/ChangeStatus/" + id, function (data) {
        $(".result").addClass("alert");
        $(".result").addClass("alert-info");
        $(".result").html(data);
    });

});

$('.deletebtn').on('click', function () {
    return confirm('Sunteți sigur ca doriti sa ștergeți această rezervare ?');
});

